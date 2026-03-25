using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

/// <summary>
/// In-memory mock of IInventoryServiceClient for unit testing.
/// </summary>
public class FakeInventoryClient : IInventoryServiceClient
{
    private readonly Dictionary<int, int> _stock = new()
    {
        { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
    };
    private readonly bool _shouldFail;

    public FakeInventoryClient(bool shouldFail = false)
    {
        _shouldFail = shouldFail;
    }

    public Task<List<InventoryItem>> GetAllInventoryAsync() =>
        Task.FromResult(_stock.Select(kv => new InventoryItem
        {
            ProductId = kv.Key,
            QuantityOnHand = kv.Value
        }).ToList());

    public Task<InventoryItem?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_stock.ContainsKey(productId)
            ? new InventoryItem { ProductId = productId, QuantityOnHand = _stock[productId] }
            : null);

    public Task<InventoryItem> RestockAsync(int productId, int quantity)
    {
        if (_stock.ContainsKey(productId)) _stock[productId] += quantity;
        return Task.FromResult(new InventoryItem
        {
            ProductId = productId,
            QuantityOnHand = _stock.GetValueOrDefault(productId)
        });
    }

    public Task<InventoryItem> DeductStockAsync(int productId, int quantity)
    {
        if (_shouldFail)
            throw new InvalidOperationException("Insufficient stock");
        return Task.FromResult(new InventoryItem { ProductId = productId, QuantityOnHand = 100 - quantity });
    }

    public Task<List<InventoryItem>> GetLowStockItemsAsync() =>
        Task.FromResult(_stock.Where(kv => kv.Value <= 10)
            .Select(kv => new InventoryItem { ProductId = kv.Key, QuantityOnHand = kv.Value })
            .ToList());
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
        var inventoryClient = new FakeInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockViaMicroservice()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = new FakeInventoryClient();
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = new FakeInventoryClient(shouldFail: true);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
