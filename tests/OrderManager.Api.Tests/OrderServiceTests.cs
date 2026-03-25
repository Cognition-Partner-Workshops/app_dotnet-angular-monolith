using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using System.Net;
using System.Net.Http.Json;

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

    private static InventoryHttpClient CreateInventoryClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5002") };
        return new InventoryHttpClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var handler = new FakeInventoryHandler();
        var inventoryClient = CreateInventoryClient(handler);
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

        var handler = new FakeInventoryHandler(stockLevel: 100, deductSuccess: true);
        var inventoryClient = CreateInventoryClient(handler);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });
        Assert.Single(order.Items);
        Assert.True(handler.DeductCalled);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var handler = new FakeInventoryHandler(stockLevel: 0, deductSuccess: false);
        var inventoryClient = CreateInventoryClient(handler);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

public class FakeInventoryHandler : HttpMessageHandler
{
    private readonly int _stockLevel;
    private readonly bool _deductSuccess;
    public bool DeductCalled { get; private set; }

    public FakeInventoryHandler(int stockLevel = 100, bool deductSuccess = true)
    {
        _stockLevel = stockLevel;
        _deductSuccess = deductSuccess;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/stock-level"))
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = JsonContent.Create(new { productId = 1, quantityOnHand = _stockLevel });
            return Task.FromResult(response);
        }

        if (path.Contains("/deduct"))
        {
            DeductCalled = true;
            if (_deductSuccess)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = JsonContent.Create(new { id = 1, productId = 1, quantityOnHand = _stockLevel - 5 });
                return Task.FromResult(response);
            }
            else
            {
                var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                response.Content = JsonContent.Create(new { error = "Insufficient stock" });
                return Task.FromResult(response);
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
