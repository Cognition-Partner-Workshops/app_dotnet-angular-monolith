using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
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

    private static InventoryServiceClient CreateMockInventoryClient(bool stockAvailable = true)
    {
        var handler = new MockInventoryHttpHandler(stockAvailable);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5002") };
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<InventoryServiceClient>();
        return new InventoryServiceClient(httpClient, logger);
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
    public async Task CreateOrder_DeductsStockViaInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient(stockAvailable: true);
        var service = new OrderService(context, inventoryClient);
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
        var inventoryClient = CreateMockInventoryClient(stockAvailable: false);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

/// <summary>
/// Mock HTTP handler that simulates inventory service responses for testing.
/// </summary>
internal class MockInventoryHttpHandler : HttpMessageHandler
{
    private readonly bool _stockAvailable;

    public MockInventoryHttpHandler(bool stockAvailable = true)
    {
        _stockAvailable = stockAvailable;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri?.PathAndQuery ?? "";

        if (uri.Contains("/check"))
        {
            var checkResult = new { productId = 1, quantity = 5, available = _stockAvailable };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(checkResult)
            });
        }

        if (uri.Contains("/deduct"))
        {
            if (!_stockAvailable)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = JsonContent.Create(new { error = "Insufficient stock" })
                });
            }
            var item = new InventoryItemDto
            {
                Id = 1, ProductId = 1, ProductName = "Test Product",
                QuantityOnHand = 95, ReorderLevel = 10, WarehouseLocation = "A-01"
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(item)
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<InventoryItemDto>())
        });
    }
}
