using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

    private static InventoryHttpClient CreateMockInventoryClient(HttpStatusCode statusCode = HttpStatusCode.OK, InventoryItem? responseItem = null)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseItem);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5100")
        };
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

        var fakeItem = new InventoryItem
        {
            Id = 1,
            ProductId = product.Id,
            QuantityOnHand = 45,
            ReorderLevel = 10,
            WarehouseLocation = "A-01"
        };
        var inventoryClient = CreateMockInventoryClient(HttpStatusCode.OK, fakeItem);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Id, order.Items.First().ProductId);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = CreateMockInventoryClient(HttpStatusCode.Conflict);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly InventoryItem? _responseItem;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, InventoryItem? responseItem = null)
    {
        _statusCode = statusCode;
        _responseItem = responseItem;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode);

        if (_statusCode == HttpStatusCode.OK && _responseItem != null)
        {
            response.Content = JsonContent.Create(_responseItem);
        }
        else if (_statusCode == HttpStatusCode.Conflict)
        {
            response.Content = JsonContent.Create(new { error = "Insufficient stock" });
        }
        else if (_statusCode == HttpStatusCode.NotFound)
        {
            response.Content = JsonContent.Create(new { error = "No inventory record" });
        }
        else if (_statusCode == HttpStatusCode.OK)
        {
            response.Content = JsonContent.Create(new List<InventoryItem>());
        }

        return Task.FromResult(response);
    }
}
