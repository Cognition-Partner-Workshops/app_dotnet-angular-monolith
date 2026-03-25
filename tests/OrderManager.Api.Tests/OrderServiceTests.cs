using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

/// <summary>
/// In-memory mock of IInventoryServiceClient that simulates stock for products 1-5.
/// </summary>
public class MockInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, int> _stock = new()
    {
        { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
    };

    public Task<List<InventoryItemDto>> GetAllInventoryAsync()
    {
        var items = _stock.Select(kvp => new InventoryItemDto
        {
            Id = kvp.Key, ProductId = kvp.Key, ProductName = $"Product {kvp.Key}",
            QuantityOnHand = kvp.Value, ReorderLevel = 10, WarehouseLocation = $"A-{kvp.Key:D2}",
            LastRestocked = DateTime.UtcNow
        }).ToList();
        return Task.FromResult(items);
    }

    public Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        if (!_stock.ContainsKey(productId)) return Task.FromResult<InventoryItemDto?>(null);
        return Task.FromResult<InventoryItemDto?>(new InventoryItemDto
        {
            Id = productId, ProductId = productId, ProductName = $"Product {productId}",
            QuantityOnHand = _stock[productId], ReorderLevel = 10, WarehouseLocation = $"A-{productId:D2}",
            LastRestocked = DateTime.UtcNow
        });
    }

    public Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId))
            throw new ArgumentException($"No inventory record for product {productId}");
        _stock[productId] += quantity;
        return Task.FromResult(new InventoryItemDto
        {
            Id = productId, ProductId = productId, ProductName = $"Product {productId}",
            QuantityOnHand = _stock[productId], ReorderLevel = 10, WarehouseLocation = $"A-{productId:D2}",
            LastRestocked = DateTime.UtcNow
        });
    }

    public Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        var items = _stock.Where(kvp => kvp.Value <= 10).Select(kvp => new InventoryItemDto
        {
            Id = kvp.Key, ProductId = kvp.Key, ProductName = $"Product {kvp.Key}",
            QuantityOnHand = kvp.Value, ReorderLevel = 10, WarehouseLocation = $"A-{kvp.Key:D2}",
            LastRestocked = DateTime.UtcNow
        }).ToList();
        return Task.FromResult(items);
    }

    public Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId))
            throw new ArgumentException($"No inventory record for product {productId}");
        if (_stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult<InventoryItemDto?>(new InventoryItemDto
        {
            Id = productId, ProductId = productId, ProductName = $"Product {productId}",
            QuantityOnHand = _stock[productId], ReorderLevel = 10, WarehouseLocation = $"A-{productId:D2}",
            LastRestocked = DateTime.UtcNow
        });
    }
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
        var inventoryClient = new MockInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryServiceToDeductStock()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
