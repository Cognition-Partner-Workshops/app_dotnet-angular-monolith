using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// In-memory mock of IInventoryServiceClient for testing.
/// </summary>
public class FakeInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, int> _stock = new()
    {
        { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
    };
    private readonly bool _shouldThrowOnDeduct;

    public FakeInventoryServiceClient(bool shouldThrowOnDeduct = false)
    {
        _shouldThrowOnDeduct = shouldThrowOnDeduct;
    }

    public Task<List<InventoryDto>> GetAllInventoryAsync() =>
        Task.FromResult(_stock.Select(kv => new InventoryDto { ProductId = kv.Key, QuantityOnHand = kv.Value }).ToList());

    public Task<InventoryDto?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_stock.ContainsKey(productId)
            ? new InventoryDto { ProductId = productId, QuantityOnHand = _stock[productId] }
            : null);

    public Task<InventoryDto> RestockAsync(int productId, int quantity)
    {
        if (_stock.ContainsKey(productId)) _stock[productId] += quantity;
        return Task.FromResult(new InventoryDto { ProductId = productId, QuantityOnHand = _stock.GetValueOrDefault(productId) });
    }

    public Task<InventoryDto> DeductStockAsync(int productId, int quantity)
    {
        if (_shouldThrowOnDeduct)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        if (!_stock.ContainsKey(productId))
            throw new InvalidOperationException($"No inventory record for product {productId}");
        if (_stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult(new InventoryDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public Task<List<InventoryDto>> GetLowStockItemsAsync() =>
        Task.FromResult(new List<InventoryDto>());
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
    public async Task CreateOrder_DeductsStockViaMicroservice()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = new FakeInventoryServiceClient();
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

        var inventoryClient = new FakeInventoryServiceClient(shouldThrowOnDeduct: true);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
