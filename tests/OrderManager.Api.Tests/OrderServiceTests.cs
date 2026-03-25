using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using OrderManager.Api.Data;
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

    private static InventoryServiceClient CreateInventoryServiceClient(
        HttpStatusCode reservationStatus = HttpStatusCode.OK,
        bool reservationSuccess = true,
        HttpStatusCode deductStatus = HttpStatusCode.OK)
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                var url = request.RequestUri?.PathAndQuery ?? "";

                if (url.Contains("check-and-reserve"))
                {
                    var response = new HttpResponseMessage(reservationStatus);
                    var json = JsonSerializer.Serialize(new { Success = reservationSuccess, Message = reservationSuccess ? "OK" : "Insufficient stock", Details = new object[0] });
                    response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    return response;
                }

                if (url.Contains("/deduct"))
                {
                    var response = new HttpResponseMessage(deductStatus);
                    if (deductStatus == HttpStatusCode.OK)
                    {
                        var json = JsonSerializer.Serialize(new { Id = 1, ProductId = 1, ProductName = "Test", Sku = "", QuantityOnHand = 10, ReorderLevel = 5, WarehouseLocation = "A-01", LastRestocked = DateTime.UtcNow });
                        response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    }
                    return response;
                }

                // Default: return OK with empty JSON
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
                };
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:5002") };
        var logger = new Mock<ILogger<InventoryServiceClient>>();
        return new InventoryServiceClient(httpClient, logger.Object);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateInventoryServiceClient();
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

        var inventoryClient = CreateInventoryServiceClient(reservationSuccess: true);
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

        var inventoryClient = CreateInventoryServiceClient(reservationSuccess: false);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenReservationFails()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        // Reservation returns success=false
        var inventoryClient = CreateInventoryServiceClient(reservationSuccess: false);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) }));
    }
}
