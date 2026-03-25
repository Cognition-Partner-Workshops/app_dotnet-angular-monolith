using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Clients;
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

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var handler = new FakeInventoryHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var inventoryClient = new InventoryServiceClient(httpClient);
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var handler = new FakeInventoryHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var inventoryClient = new InventoryServiceClient(httpClient);
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
        var handler = new FakeInventoryHandler(insufficientStock: true);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var inventoryClient = new InventoryServiceClient(httpClient);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

internal class FakeInventoryHandler : HttpMessageHandler
{
    private readonly bool _insufficientStock;

    public FakeInventoryHandler(bool insufficientStock = false)
    {
        _insufficientStock = insufficientStock;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/check"))
        {
            var result = new { productId = 1, quantity = 5, available = !_insufficientStock };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(result)
            });
        }

        if (path.Contains("/deduct"))
        {
            if (_insufficientStock)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = JsonContent.Create(new { error = "Insufficient stock" })
                });
            }
            var item = new { id = 1, productId = 1, productName = "Widget A", sku = "WGT-001", quantityOnHand = 45, reorderLevel = 10, warehouseLocation = "A-01", lastRestocked = DateTime.UtcNow };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(item)
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<object>())
        });
    }
}
