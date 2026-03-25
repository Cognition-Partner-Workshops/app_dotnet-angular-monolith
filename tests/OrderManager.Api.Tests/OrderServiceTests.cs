using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

    private static InventoryService CreateInventoryService(HttpStatusCode deductStatus, object? deductBody = null)
    {
        var handler = new FakeHttpMessageHandler(deductStatus, deductBody);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5100") };
        return new InventoryService(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryService = CreateInventoryService(HttpStatusCode.OK);
        var service = new OrderService(context, inventoryService);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockViaMicroservice()
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
        var inventoryService = CreateInventoryService(HttpStatusCode.OK, deductResponse);
        var service = new OrderService(context, inventoryService);

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

        // inventory-service returns 500 on insufficient stock → EnsureSuccessStatusCode throws
        var inventoryService = CreateInventoryService(HttpStatusCode.InternalServerError);
        var service = new OrderService(context, inventoryService);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly object? _responseBody;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, object? responseBody = null)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode);
        if (_responseBody != null)
        {
            response.Content = new StringContent(
                JsonSerializer.Serialize(_responseBody),
                System.Text.Encoding.UTF8,
                "application/json");
        }
        return Task.FromResult(response);
    }
}
