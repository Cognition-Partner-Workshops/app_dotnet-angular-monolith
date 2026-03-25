using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using System.Net;
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

    private static InventoryServiceClient CreateClient(HttpStatusCode statusCode = HttpStatusCode.OK, string? json = null)
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
                if (json != null)
                    response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                return response;
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:5002") };
        var logger = Mock.Of<ILogger<InventoryServiceClient>>();
        return new InventoryServiceClient(httpClient, logger);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var client = CreateClient();
        var service = new OrderService(context, client);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockViaInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryClient = new FakeInventoryClient(new Dictionary<int, int> { { product.Id, 100 } });
        var service = new OrderService(context, inventoryClient);

        var deductedItem = new InventoryItemDto
        {
            Id = 1,
            ProductId = product.Id,
            ProductName = product.Name,
            QuantityOnHand = 95,
            ReorderLevel = 10
        };
        var json = System.Text.Json.JsonSerializer.Serialize(deductedItem);
        var client = CreateClient(HttpStatusCode.OK, json);
        var service = new OrderService(context, client);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryClient(defaultStock: 2);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryClient = new FakeInventoryClient(new Dictionary<int, int> { { product.Id, 2 } });
        var service = new OrderService(context, inventoryClient);

        var client = CreateClient(HttpStatusCode.Conflict, "{\"error\":\"Insufficient stock\"}");
        var service = new OrderService(context, client);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

public class FakeInventoryHandler : HttpMessageHandler
{
    private readonly bool _stockAvailable;

    public FakeInventoryHandler(bool stockAvailable = true)
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryClient(new Dictionary<int, int>());
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/check"))
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { productId = 1, quantity = 1, available = _stockAvailable })
            };
            return Task.FromResult(response);
        }

        if (path.Contains("/deduct"))
        {
            if (!_stockAvailable)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict));

            var dto = new InventoryItemDto
            {
                Id = 1, ProductId = 1, ProductName = "Widget A",
                QuantityOnHand = 45, ReorderLevel = 10,
                WarehouseLocation = "A-01", LastRestocked = DateTime.UtcNow
            };
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dto)
            };
            return Task.FromResult(response);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
