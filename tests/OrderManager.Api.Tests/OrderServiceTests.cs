using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

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
        var mockClient = new Mock<IInventoryServiceClient>();
        var service = new OrderService(context, mockClient.Object);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockViaMicroservice()
    {
        using var context = CreateContext();
        var service = new OrderService(context, new FakeInventoryServiceClient());
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var mockClient = new Mock<IInventoryServiceClient>();
        mockClient.Setup(c => c.DeductStockAsync(product.Id, 5))
            .ReturnsAsync(new InventoryItem
            {
                Id = 1, ProductId = product.Id, ProductName = product.Name,
                QuantityOnHand = 45, ReorderLevel = 10, WarehouseLocation = "A-01"
            });

        var service = new OrderService(context, mockClient.Object);
        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var service = new OrderService(context, new FakeInventoryServiceClient(shouldThrowOnDeduct: true));
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var mockClient = new Mock<IInventoryServiceClient>();
        mockClient.Setup(c => c.DeductStockAsync(product.Id, 99999))
            .ThrowsAsync(new InvalidOperationException("Insufficient stock"));

        var service = new OrderService(context, mockClient.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
