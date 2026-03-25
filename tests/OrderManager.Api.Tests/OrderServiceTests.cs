using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class MockInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, int> _stock = new();
    private readonly bool _shouldThrowOnDeduct;

    public MockInventoryServiceClient(bool shouldThrowOnDeduct = false)
    {
        _shouldThrowOnDeduct = shouldThrowOnDeduct;
        _stock[1] = 50;
        _stock[2] = 30;
        _stock[3] = 75;
    }

    public Task<List<InventoryDto>> GetAllInventoryAsync()
    {
        var items = _stock.Select(kvp => new InventoryDto(
            kvp.Key, kvp.Key, $"Product {kvp.Key}", $"SKU-{kvp.Key}",
            kvp.Value, 10, "A-01", DateTime.UtcNow)).ToList();
        return Task.FromResult(items);
    }

    public Task<InventoryDto?> GetInventoryByProductIdAsync(int productId)
    {
        if (_stock.TryGetValue(productId, out var qty))
            return Task.FromResult<InventoryDto?>(new InventoryDto(
                productId, productId, $"Product {productId}", $"SKU-{productId}",
                qty, 10, "A-01", DateTime.UtcNow));
        return Task.FromResult<InventoryDto?>(null);
    }

    public Task<InventoryDto> RestockAsync(int productId, int quantity)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
        return Task.FromResult(new InventoryDto(
            productId, productId, $"Product {productId}", $"SKU-{productId}",
            _stock[productId], 10, "A-01", DateTime.UtcNow));
    }

    public Task<List<InventoryDto>> GetLowStockItemsAsync()
    {
        var items = _stock.Where(kvp => kvp.Value <= 10)
            .Select(kvp => new InventoryDto(
                kvp.Key, kvp.Key, $"Product {kvp.Key}", $"SKU-{kvp.Key}",
                kvp.Value, 10, "A-01", DateTime.UtcNow)).ToList();
        return Task.FromResult(items);
    }

    public Task<InventoryDto?> DeductStockAsync(int productId, int quantity)
    {
        if (_shouldThrowOnDeduct || !_stock.ContainsKey(productId) || _stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");

        _stock[productId] -= quantity;
        return Task.FromResult<InventoryDto?>(new InventoryDto(
            productId, productId, $"Product {productId}", $"SKU-{productId}",
            _stock[productId], 10, "A-01", DateTime.UtcNow));
    }

    public Task<int> GetStockLevelAsync(int productId)
    {
        return Task.FromResult(_stock.GetValueOrDefault(productId));
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
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryServiceClient();
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
        var inventoryClient = new MockInventoryServiceClient(shouldThrowOnDeduct: true);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
