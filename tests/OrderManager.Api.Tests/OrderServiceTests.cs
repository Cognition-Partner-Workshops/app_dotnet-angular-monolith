using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

    private static InventoryHttpClient CreateMockInventoryClient(HttpStatusCode statusCode = HttpStatusCode.OK, object? responseBody = null)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5002") };
        return new InventoryHttpClient(httpClient);
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
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var deductResponse = new InventoryItemDto
        {
            Id = 1,
            ProductId = product.Id,
            ProductName = product.Name,
            QuantityOnHand = 45,
            ReorderLevel = 10,
            WarehouseLocation = "A-01"
        };
        var inventoryClient = CreateMockInventoryClient(HttpStatusCode.OK, deductResponse);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Id, order.Items.First().ProductId);
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenInventoryServiceReturnsError()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var errorResponse = new { error = "Insufficient stock" };
        var inventoryClient = CreateMockInventoryClient(HttpStatusCode.BadRequest, errorResponse);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly object? _responseBody;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, object? responseBody)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode);
        if (_responseBody is not null)
        {
            response.Content = new StringContent(
                JsonSerializer.Serialize(_responseBody),
                System.Text.Encoding.UTF8,
                "application/json");
        }
        return Task.FromResult(response);
    }
}
