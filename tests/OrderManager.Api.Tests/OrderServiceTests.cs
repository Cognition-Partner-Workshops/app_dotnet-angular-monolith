using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using OrderManager.Api.Data;
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

    private static InventoryServiceClient CreateInventoryClient(HttpStatusCode statusCode = HttpStatusCode.OK, string? jsonContent = null)
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var response = new HttpResponseMessage(statusCode);
                if (jsonContent != null)
                    response.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                return response;
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:5002") };
        var logger = new Mock<ILogger<InventoryServiceClient>>();
        return new InventoryServiceClient(httpClient, logger.Object);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateInventoryClient();
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

        var inventoryClient = CreateInventoryClient(HttpStatusCode.OK,
            JsonSerializer.Serialize(new { success = true, message = "", details = new List<object>() }));
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

        var inventoryClient = CreateInventoryClient(HttpStatusCode.InternalServerError);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
