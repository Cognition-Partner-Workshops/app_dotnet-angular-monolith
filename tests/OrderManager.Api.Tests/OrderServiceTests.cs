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

    private static InventoryHttpClient CreateMockInventoryClient(
        Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var mockHandler = new MockHttpMessageHandler(handler);
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("http://localhost:5100")
        };
        return new InventoryHttpClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));
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
        var decrementCalled = false;

        var inventoryClient = CreateMockInventoryClient(request =>
        {
            if (request.RequestUri!.PathAndQuery.Contains($"/api/inventory/product/{product.Id}/deduct"))
            {
                decrementCalled = true;
                var item = new InventoryItem
                {
                    Id = 1, ProductId = product.Id, ProductName = product.Name,
                    QuantityOnHand = 45, ReorderLevel = 10
                };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(item)
                };
            }

            if (request.RequestUri.PathAndQuery.Contains($"/api/inventory/product/{product.Id}"))
            {
                var item = new InventoryItem
                {
                    Id = 1, ProductId = product.Id, ProductName = product.Name,
                    QuantityOnHand = 50, ReorderLevel = 10
                };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(item)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var service = new OrderService(context, inventoryClient);
        await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.True(decrementCalled, "Inventory decrement should be called via HTTP");
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = CreateMockInventoryClient(request =>
        {
            if (request.RequestUri!.PathAndQuery.Contains($"/api/inventory/product/{product.Id}"))
            {
                var item = new InventoryItem
                {
                    Id = 1, ProductId = product.Id, ProductName = product.Name,
                    QuantityOnHand = 5, ReorderLevel = 10
                };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(item)
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var service = new OrderService(context, inventoryClient);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }
}
