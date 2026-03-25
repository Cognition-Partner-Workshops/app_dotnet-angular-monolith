using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
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

    private static InventoryApiClient CreateMockInventoryClient(bool stockAvailable = true)
    {
        var handler = new MockHttpMessageHandler(stockAvailable);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5100")
        };
        return new InventoryApiClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient(stockAvailable: true);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient(stockAvailable: false);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

/// <summary>
/// Mock HTTP handler that simulates the inventory microservice responses.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly bool _stockAvailable;

    public MockHttpMessageHandler(bool stockAvailable)
    {
        _stockAvailable = stockAvailable;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;

        if (path.Contains("/check"))
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { productId = 1, quantity = 5, available = _stockAvailable })
            };
            return Task.FromResult(response);
        }

        if (path.Contains("/deduct"))
        {
            if (!_stockAvailable)
            {
                var conflictResponse = new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = JsonContent.Create(new { error = "Insufficient stock" })
                };
                return Task.FromResult(conflictResponse);
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new InventoryItemDto
                {
                    Id = 1, ProductId = 1, ProductName = "Widget A",
                    QuantityOnHand = 45, ReorderLevel = 10,
                    WarehouseLocation = "A-01", LastRestocked = DateTime.UtcNow
                })
            };
            return Task.FromResult(response);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
