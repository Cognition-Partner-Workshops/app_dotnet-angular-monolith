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

    private static InventoryApiClient CreateMockInventoryClient(HttpStatusCode statusCode, InventoryItem? responseItem)
    {
        var handler = new MockHttpMessageHandler(statusCode, responseItem);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5100") };
        return new InventoryApiClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient(HttpStatusCode.OK, null);
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_SucceedsWhenInventoryAvailable()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var mockItem = new InventoryItem
        {
            Id = 1, ProductId = product.Id, ProductName = product.Name,
            ProductSku = product.Sku, QuantityOnHand = 45, ReorderLevel = 10,
            WarehouseLocation = "A-01"
        };
        var inventoryClient = CreateMockInventoryClient(HttpStatusCode.OK, mockItem);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });
        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenInventoryNotFound()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = CreateMockInventoryClient(HttpStatusCode.OK, null);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) }));
    }
}

internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly InventoryItem? _responseItem;

    public MockHttpMessageHandler(HttpStatusCode statusCode, InventoryItem? responseItem)
    {
        _statusCode = statusCode;
        _responseItem = responseItem;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode);
        if (_responseItem is not null)
        {
            response.Content = new StringContent(
                JsonSerializer.Serialize(_responseItem),
                System.Text.Encoding.UTF8,
                "application/json");
        }
        else
        {
            response.Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json");
        }
        return Task.FromResult(response);
    }
}
