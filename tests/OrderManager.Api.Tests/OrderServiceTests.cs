using Microsoft.EntityFrameworkCore;
using Moq;
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

    private static Mock<InventoryHttpClient> CreateMockInventoryClient()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:5100") };
        return new Mock<InventoryHttpClient>(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var mockInventoryClient = CreateMockInventoryClient();
        var service = new OrderService(context, mockInventoryClient.Object);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryServiceToDeductStock()
    {
        using var context = CreateContext();
        var mockInventoryClient = CreateMockInventoryClient();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        mockInventoryClient
            .Setup(c => c.DeductStockAsync(product.Id, 5))
            .ReturnsAsync(new InventoryItemDto
            {
                Id = 1,
                ProductId = product.Id,
                ProductName = product.Name,
                QuantityOnHand = 45,
                ReorderLevel = 10,
                WarehouseLocation = "A-01"
            });

        var service = new OrderService(context, mockInventoryClient.Object);
        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        mockInventoryClient.Verify(c => c.DeductStockAsync(product.Id, 5), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var mockInventoryClient = CreateMockInventoryClient();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        mockInventoryClient
            .Setup(c => c.DeductStockAsync(product.Id, 99999))
            .ThrowsAsync(new InvalidOperationException("Insufficient stock"));

        var service = new OrderService(context, mockInventoryClient.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
