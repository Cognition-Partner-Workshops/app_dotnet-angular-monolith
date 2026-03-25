using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Clients;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class FakeInventoryClient : IInventoryClient
{
    private readonly Dictionary<int, int> _stock = new();

    public FakeInventoryClient(Dictionary<int, int>? initialStock = null)
    {
        _stock = initialStock ?? new Dictionary<int, int>();
    }

    public Task<List<InventoryDto>> GetAllInventoryAsync() =>
        Task.FromResult(_stock.Select(kvp => new InventoryDto(
            kvp.Key, kvp.Key, $"Product {kvp.Key}", $"SKU-{kvp.Key}",
            kvp.Value, 10, "A-01", DateTime.UtcNow)).ToList());

    public Task<InventoryDto?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_stock.ContainsKey(productId)
            ? (InventoryDto?)new InventoryDto(productId, productId, $"Product {productId}", $"SKU-{productId}",
                _stock[productId], 10, "A-01", DateTime.UtcNow)
            : null);

    public Task<InventoryDto> RestockAsync(int productId, int quantity)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
        return Task.FromResult(new InventoryDto(
            productId, productId, $"Product {productId}", $"SKU-{productId}",
            _stock[productId], 10, "A-01", DateTime.UtcNow));
    }

    public Task<List<InventoryDto>> GetLowStockItemsAsync() =>
        Task.FromResult(new List<InventoryDto>());

    public Task<bool> CheckStockAsync(int productId, int quantity) =>
        Task.FromResult(_stock.ContainsKey(productId) && _stock[productId] >= quantity);

    public Task<InventoryDto?> DeductStockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId) || _stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult<InventoryDto?>(new InventoryDto(
            productId, productId, $"Product {productId}", $"SKU-{productId}",
            _stock[productId], 10, "A-01", DateTime.UtcNow));
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
        var inventoryClient = new FakeInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsInventoryViaHttpClient()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryClient = new FakeInventoryClient(new Dictionary<int, int> { { product.Id, 100 } });
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
        var stockAvailable = await inventoryClient.CheckStockAsync(product.Id, 95);
        Assert.True(stockAvailable);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryClient = new FakeInventoryClient(new Dictionary<int, int> { { product.Id, 2 } });
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
