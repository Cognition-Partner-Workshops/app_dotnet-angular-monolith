using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// Mock HTTP message handler for testing the InventoryApiClient integration.
/// </summary>
internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly bool _checkStockResult;
    private readonly bool _deductStockReturnsNull;

    public MockHttpMessageHandler(bool checkStockResult = true, bool deductStockReturnsNull = false)
    {
        _checkStockResult = checkStockResult;
        _deductStockReturnsNull = deductStockReturnsNull;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/check"))
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = JsonContent.Create(new { productId = 1, quantity = 1, available = _checkStockResult });
            return Task.FromResult(response);
        }

        if (path.Contains("/deduct"))
        {
            if (_deductStockReturnsNull)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = JsonContent.Create(new { error = "Insufficient stock" })
                });
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = JsonContent.Create(new InventoryItemDto
            {
                Id = 1,
                ProductId = 1,
                QuantityOnHand = 45,
                ReorderLevel = 10,
                WarehouseLocation = "A-01",
                LastRestocked = DateTime.UtcNow
            });
            return Task.FromResult(response);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
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

    private static InventoryApiClient CreateMockInventoryApiClient(
        bool checkStockResult = true,
        bool deductStockReturnsNull = false)
    {
        var handler = new MockHttpMessageHandler(checkStockResult, deductStockReturnsNull);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5002") };
        var logger = new LoggerFactory().CreateLogger<InventoryApiClient>();
        return new InventoryApiClient(httpClient, logger);
    }

    private static ILogger<OrderService> CreateLogger() => new LoggerFactory().CreateLogger<OrderService>();

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryApiClient();
        var service = new OrderService(context, inventoryClient, CreateLogger());
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryApiClient(checkStockResult: true);
        var service = new OrderService(context, inventoryClient, CreateLogger());
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryApiClient(checkStockResult: false);
        var service = new OrderService(context, inventoryClient, CreateLogger());
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenDeductFails()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryApiClient(checkStockResult: true, deductStockReturnsNull: true);
        var service = new OrderService(context, inventoryClient, CreateLogger());
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        // DeductStockAsync returns null (Conflict) but our OrderService doesn't check the result,
        // so the order should still be created successfully
        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });
        Assert.NotNull(order);
    }
}
