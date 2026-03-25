using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

/// <summary>
/// Stub implementation of IInventoryServiceClient for testing.
/// Tracks deductions in memory to verify order-inventory integration.
/// </summary>
public class StubInventoryClient : IInventoryServiceClient
{
    private readonly Dictionary<int, int> _stock = new()
    {
        { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
    };

    public Task<List<InventoryDto>> GetAllInventoryAsync() =>
        Task.FromResult(_stock.Select(kvp => new InventoryDto(
            Id: kvp.Key,
            ProductId: kvp.Key,
            ProductName: $"Product {kvp.Key}",
            Sku: $"SKU-{kvp.Key}",
            QuantityOnHand: kvp.Value,
            ReorderLevel: 10,
            WarehouseLocation: $"A-{kvp.Key:D2}",
            LastRestocked: DateTime.UtcNow
        )).ToList());

    public Task<InventoryDto?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_stock.ContainsKey(productId)
            ? new InventoryDto(productId, productId, $"Product {productId}", $"SKU-{productId}",
                _stock[productId], 10, $"A-{productId:D2}", DateTime.UtcNow)
            : null);

    public Task<InventoryDto> RestockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId))
            throw new ArgumentException($"No inventory record for product {productId}");
        _stock[productId] += quantity;
        return Task.FromResult(new InventoryDto(productId, productId, $"Product {productId}", $"SKU-{productId}",
            _stock[productId], 10, $"A-{productId:D2}", DateTime.UtcNow));
    }

    public Task<InventoryDto?> DeductStockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId))
            throw new ArgumentException($"No inventory record for product {productId}");
        if (_stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}. Available: {_stock[productId]}");
        _stock[productId] -= quantity;
        return Task.FromResult<InventoryDto?>(new InventoryDto(productId, productId, $"Product {productId}", $"SKU-{productId}",
            _stock[productId], 10, $"A-{productId:D2}", DateTime.UtcNow));
    }

    public Task<List<InventoryDto>> GetLowStockItemsAsync() =>
        Task.FromResult(_stock.Where(kvp => kvp.Value <= 10)
            .Select(kvp => new InventoryDto(kvp.Key, kvp.Key, $"Product {kvp.Key}", $"SKU-{kvp.Key}",
                kvp.Value, 10, $"A-{kvp.Key:D2}", DateTime.UtcNow))
            .ToList());

    public Task<int> GetStockLevelAsync(int productId) =>
        Task.FromResult(_stock.GetValueOrDefault(productId, 0));

    public int GetStock(int productId) => _stock.GetValueOrDefault(productId, 0);
}

public class OrderServiceTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        SeedData.Initialize(context);
        return context;
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = new StubInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsInventoryViaClient()
    {
        using var context = CreateContext();
        var inventoryClient = new StubInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var qtyBefore = inventoryClient.GetStock(product.Id);

        await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.Equal(qtyBefore - 5, inventoryClient.GetStock(product.Id));
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = new StubInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
