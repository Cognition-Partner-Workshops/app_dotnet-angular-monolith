using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

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
        var inventoryClient = new FakeInventoryServiceClient(deductSucceeds: true);
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockViaInventoryService()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = new FakeInventoryServiceClient(deductSucceeds: true);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = new FakeInventoryServiceClient(deductSucceeds: false);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

/// <summary>
/// Fake implementation of IInventoryServiceClient for unit testing.
/// </summary>
public class FakeInventoryServiceClient : IInventoryServiceClient
{
    private readonly bool _deductSucceeds;

    public FakeInventoryServiceClient(bool deductSucceeds = true)
    {
        _deductSucceeds = deductSucceeds;
    }

    public Task<List<InventoryItem>> GetAllInventoryAsync()
        => Task.FromResult(new List<InventoryItem>());

    public Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
        => Task.FromResult<InventoryItem?>(new InventoryItem
        {
            Id = 1, ProductId = productId, ProductName = "Test",
            QuantityOnHand = 100, ReorderLevel = 10, WarehouseLocation = "A-01"
        });

    public Task<InventoryItem> RestockAsync(int productId, int quantity)
        => Task.FromResult(new InventoryItem
        {
            Id = 1, ProductId = productId, ProductName = "Test",
            QuantityOnHand = 100 + quantity, ReorderLevel = 10, WarehouseLocation = "A-01"
        });

    public Task<List<InventoryItem>> GetLowStockItemsAsync()
        => Task.FromResult(new List<InventoryItem>());

    public Task<InventoryItem> DeductStockAsync(int productId, int quantity)
    {
        if (!_deductSucceeds)
            throw new InvalidOperationException("Insufficient stock");

        return Task.FromResult(new InventoryItem
        {
            Id = 1, ProductId = productId, ProductName = "Test",
            QuantityOnHand = 100 - quantity, ReorderLevel = 10, WarehouseLocation = "A-01"
        });
    }
}
