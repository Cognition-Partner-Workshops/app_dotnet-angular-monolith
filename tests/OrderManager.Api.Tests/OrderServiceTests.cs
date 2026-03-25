using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class FakeInventoryClient : IInventoryServiceClient
{
    private readonly bool _shouldFail;

    public FakeInventoryClient(bool shouldFail = false)
    {
        _shouldFail = shouldFail;
    }

    public Task<List<InventoryItem>> GetAllInventoryAsync() => Task.FromResult(new List<InventoryItem>());
    public Task<InventoryItem?> GetInventoryByProductIdAsync(int productId) => Task.FromResult<InventoryItem?>(null);
    public Task<InventoryItem> RestockAsync(int productId, int quantity) => Task.FromResult(new InventoryItem { ProductId = productId, QuantityOnHand = quantity });
    public Task<List<InventoryItem>> GetLowStockItemsAsync() => Task.FromResult(new List<InventoryItem>());

    public Task<InventoryItem> DeductStockAsync(int productId, int quantity)
    {
        if (_shouldFail)
            throw new InvalidOperationException("Insufficient stock");
        return Task.FromResult(new InventoryItem { ProductId = productId, QuantityOnHand = 100 - quantity });
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
        var service = new OrderService(context, new FakeInventoryClient());
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockViaMicroservice()
    {
        using var context = CreateContext();
        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var service = new OrderService(context, new FakeInventoryClient());
        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var mockClient = new Mock<IInventoryServiceClient>();
        mockClient.Setup(c => c.DeductStockAsync(product.Id, 99999))
            .ThrowsAsync(new InvalidOperationException("Insufficient stock"));

        var service = new OrderService(context, mockClient.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenProductNotInInventory()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var mockClient = new Mock<IInventoryServiceClient>();
        mockClient.Setup(c => c.DeductStockAsync(product.Id, 1))
            .ThrowsAsync(new ArgumentException($"No inventory record for product {product.Id}"));

        var service = new OrderService(context, mockClient.Object);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 1) }));
    }
}
