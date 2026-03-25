using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Clients;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// In-memory mock of IInventoryClient for unit testing.
/// Simulates inventory stock levels without HTTP calls.
/// </summary>
public class MockInventoryClient : IInventoryClient
{
    private readonly Dictionary<int, int> _stock = new();

    public MockInventoryClient(int defaultStock = 50)
    {
        for (int i = 1; i <= 5; i++)
            _stock[i] = defaultStock;
    }

    public MockInventoryClient(Dictionary<int, int> initialStock)
    {
        foreach (var kvp in initialStock)
            _stock[kvp.Key] = kvp.Value;
    }

    public Task<List<InventoryItemDto>> GetAllInventoryAsync()
        => Task.FromResult(_stock.Select(kvp => new InventoryItemDto { ProductId = kvp.Key, QuantityOnHand = kvp.Value }).ToList());

    public Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
        => Task.FromResult(_stock.ContainsKey(productId)
            ? new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] }
            : (InventoryItemDto?)null);

    public Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
        return Task.FromResult(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public Task<List<InventoryItemDto>> GetLowStockItemsAsync()
        => Task.FromResult(_stock.Where(kvp => kvp.Value <= 10)
            .Select(kvp => new InventoryItemDto { ProductId = kvp.Key, QuantityOnHand = kvp.Value }).ToList());

    public Task<bool> CheckStockAsync(int productId, int quantity)
        => Task.FromResult(_stock.ContainsKey(productId) && _stock[productId] >= quantity);

    public Task<InventoryItemDto> DeductStockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId) || _stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
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
        var inventoryClient = new MockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryClient(defaultStock: 2);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenProductNotInInventory()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryClient(new Dictionary<int, int>());
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 1) }));
    }
}
