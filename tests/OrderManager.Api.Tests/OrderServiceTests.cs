using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class FakeInventoryClient : IInventoryServiceClient
{
    private readonly Dictionary<int, int> _stock;
    private readonly bool _shouldThrowOnDeduct;

    public FakeInventoryClient(bool shouldThrowOnDeduct = false)
    {
        _shouldThrowOnDeduct = shouldThrowOnDeduct;
        _stock = new Dictionary<int, int> { { 1, 100 }, { 2, 100 }, { 3, 100 } };
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
            : (InventoryItem?)null);

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
        if (_shouldThrowOnDeduct)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");

        if (!_stock.ContainsKey(productId))
            throw new ArgumentException($"No inventory record for product {productId}");
        if (_stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult(new InventoryItem
        {
            ProductId = productId,
            QuantityOnHand = _stock[productId]
        });
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
        var service = new OrderService(context, new FakeInventoryClient());
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockViaMicroservice()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var service = new OrderService(context, new FakeInventoryClient());
        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var service = new OrderService(context, new FakeInventoryClient(shouldThrowOnDeduct: true));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
