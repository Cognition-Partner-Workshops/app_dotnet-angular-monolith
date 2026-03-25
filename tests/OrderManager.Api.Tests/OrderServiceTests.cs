using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

public class FakeInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, int> _stock = new()
    {
        { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
    };

    public Task<List<InventoryItemDto>> GetAllInventoryAsync() =>
        Task.FromResult(_stock.Select(kv => new InventoryItemDto { ProductId = kv.Key, QuantityOnHand = kv.Value }).ToList());

    public Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_stock.ContainsKey(productId) ? new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] } : null);

    public Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        if (_stock.ContainsKey(productId)) _stock[productId] += quantity;
        return Task.FromResult(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock.GetValueOrDefault(productId) });
    }

    public Task<List<InventoryItemDto>> GetLowStockItemsAsync() =>
        Task.FromResult(new List<InventoryItemDto>());

    public Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId))
            throw new InvalidOperationException($"No inventory record for product {productId}");
        if (_stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult<InventoryItemDto?>(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public Task<int> GetStockLevelAsync(int productId) =>
        Task.FromResult(_stock.GetValueOrDefault(productId, 0));
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
        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_SucceedsWhenStockAvailable()
    {
        using var context = CreateContext();
        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
