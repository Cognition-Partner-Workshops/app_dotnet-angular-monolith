using System.Net;
using System.Net.Http.Json;
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

    private static InventoryServiceClient CreateClient(HttpStatusCode statusCode, InventoryItem? responseItem = null)
    {
        var item = responseItem ?? new InventoryItem
        {
            Id = 1, ProductId = 1, ProductName = "Widget A",
            QuantityOnHand = 95, ReorderLevel = 10,
            WarehouseLocation = "A-01", LastRestocked = DateTime.UtcNow
        };
        string? json = statusCode == HttpStatusCode.OK
            ? System.Text.Json.JsonSerializer.Serialize(item)
            : null;
        var handler = new FakeHttpHandler(statusCode, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5100") };
        var logger = new FakeLogger();
        return new InventoryServiceClient(httpClient, logger);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateClient(HttpStatusCode.OK);
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var deductedItem = new InventoryItem
        {
            Id = 1, ProductId = product.Id, ProductName = product.Name,
            QuantityOnHand = 95, ReorderLevel = 10, WarehouseLocation = "A-01"
        };
        var inventoryClient = CreateClient(HttpStatusCode.OK, deductedItem);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenInventoryServiceReturnsConflict()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = CreateClient(HttpStatusCode.Conflict);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

public class FakeHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string? _responseJson;

    public FakeHttpHandler(HttpStatusCode statusCode, string? responseJson)
    {
        _statusCode = statusCode;
        _responseJson = responseJson;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode);
        if (_responseJson != null)
            response.Content = new StringContent(_responseJson, System.Text.Encoding.UTF8, "application/json");
        return Task.FromResult(response);
    }
}

public class FakeLogger : Microsoft.Extensions.Logging.ILogger<InventoryServiceClient>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId,
        TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
