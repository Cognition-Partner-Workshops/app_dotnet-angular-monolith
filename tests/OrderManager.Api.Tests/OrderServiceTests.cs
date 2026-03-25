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

    private static InventoryService CreateInventoryService(Dictionary<string, HttpResponseMessage> responses)
    {
        var handler = new FakeHttpMessageHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5100") };
        return new InventoryService(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryService = CreateInventoryService(new Dictionary<string, HttpResponseMessage>());
        var service = new OrderService(context, inventoryService);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var deductedItem = new InventoryItemDto
        {
            Id = 1, ProductId = product.Id, ProductName = product.Name,
            QuantityOnHand = 45, ReorderLevel = 10, WarehouseLocation = "A-01"
        };

        var responses = new Dictionary<string, HttpResponseMessage>
        {
            [$"api/inventory/product/{product.Id}/check?quantity=5"] = new(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { productId = product.Id, quantity = 5, available = true })
            },
            [$"api/inventory/product/{product.Id}/deduct"] = new(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(deductedItem)
            }
        };

        var inventoryService = CreateInventoryService(responses);
        var service = new OrderService(context, inventoryService);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });
        Assert.NotNull(order);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var responses = new Dictionary<string, HttpResponseMessage>
        {
            [$"api/inventory/product/{product.Id}/check?quantity=99999"] = new(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { productId = product.Id, quantity = 99999, available = false })
            }
        };

        var inventoryService = CreateInventoryService(responses);
        var service = new OrderService(context, inventoryService);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, HttpResponseMessage> _responses;

    public FakeHttpMessageHandler(Dictionary<string, HttpResponseMessage> responses)
    {
        _responses = responses;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery.TrimStart('/') ?? string.Empty;
        if (_responses.TryGetValue(path, out var response))
            return Task.FromResult(response);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
