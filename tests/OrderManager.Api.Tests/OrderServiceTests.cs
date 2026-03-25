using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Clients;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class MockInventoryClient : IInventoryClient
{
    private readonly Dictionary<int, int> _stock = new();
    private readonly bool _shouldThrowOnDeduct;

    public MockInventoryClient(bool shouldThrowOnDeduct = false)
    {
        _shouldThrowOnDeduct = shouldThrowOnDeduct;
        _stock[1] = 50;
        _stock[2] = 30;
        _stock[3] = 75;
    }

    public Task<List<InventoryItemDto>> GetAllInventoryAsync()
    {
        var items = _stock.Select(kvp => new InventoryItemDto
        {
            Id = kvp.Key, ProductId = kvp.Key, ProductName = $"Product {kvp.Key}",
            QuantityOnHand = kvp.Value, ReorderLevel = 10, WarehouseLocation = "A-01",
            LastRestocked = DateTime.UtcNow
        }).ToList();
        return Task.FromResult(items);
    }

    public Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        if (_stock.TryGetValue(productId, out var qty))
            return Task.FromResult<InventoryItemDto?>(new InventoryItemDto
            {
                Id = productId, ProductId = productId, ProductName = $"Product {productId}",
                QuantityOnHand = qty, ReorderLevel = 10, WarehouseLocation = "A-01",
                LastRestocked = DateTime.UtcNow
            });
        return Task.FromResult<InventoryItemDto?>(null);
    }

    public Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
        return Task.FromResult(new InventoryItemDto
        {
            Id = productId, ProductId = productId, ProductName = $"Product {productId}",
            QuantityOnHand = _stock[productId], ReorderLevel = 10, WarehouseLocation = "A-01",
            LastRestocked = DateTime.UtcNow
        });
    }

    public Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        var items = _stock.Where(kvp => kvp.Value <= 10)
            .Select(kvp => new InventoryItemDto
            {
                Id = kvp.Key, ProductId = kvp.Key, ProductName = $"Product {kvp.Key}",
                QuantityOnHand = kvp.Value, ReorderLevel = 10, WarehouseLocation = "A-01",
                LastRestocked = DateTime.UtcNow
            }).ToList();
        return Task.FromResult(items);
    }

    public Task<bool> CheckStockAsync(int productId, int quantity)
    {
        if (_stock.TryGetValue(productId, out var qty))
            return Task.FromResult(qty >= quantity);
        return Task.FromResult(false);
    }

    public Task<InventoryItemDto> DeductStockAsync(int productId, int quantity)
    {
        if (_shouldThrowOnDeduct || !_stock.ContainsKey(productId) || _stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");

        _stock[productId] -= quantity;
        return Task.FromResult(new InventoryItemDto
        {
            Id = productId, ProductId = productId, ProductName = $"Product {productId}",
            QuantityOnHand = _stock[productId], ReorderLevel = 10, WarehouseLocation = "A-01",
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
        var inventoryClient = new MockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryServiceToDeductStock()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Id, order.Items.First().ProductId);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryClient(shouldThrowOnDeduct: true);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
