using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

public class FakeInventoryClient : IInventoryServiceClient
{
    private readonly bool _shouldFail;

    public FakeInventoryClient(bool shouldFail = false)
    {
        _shouldFail = shouldFail;
    }

    public Task<List<InventoryItem>> GetAllInventoryAsync() =>
        Task.FromResult(new List<InventoryItem>());

    public Task<InventoryItem?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult<InventoryItem?>(new InventoryItem { ProductId = productId, QuantityOnHand = 100 });

    public Task<InventoryItem> RestockAsync(int productId, int quantity) =>
        Task.FromResult(new InventoryItem { ProductId = productId, QuantityOnHand = 100 + quantity });

    public Task<InventoryItem> DeductStockAsync(int productId, int quantity)
    {
        if (_shouldFail)
            throw new InvalidOperationException("Insufficient stock");
        return Task.FromResult(new InventoryItem { ProductId = productId, QuantityOnHand = 100 - quantity });
    }

    public Task<List<InventoryItem>> GetLowStockItemsAsync() =>
        Task.FromResult(new List<InventoryItem>());
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
        var inventoryClient = new FakeInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

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

        var inventoryClient = new FakeInventoryClient(shouldFail: true);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
