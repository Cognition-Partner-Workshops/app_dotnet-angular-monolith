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

    private static InventoryHttpClient CreateInventoryClient(HttpStatusCode checkStatus, bool stockAvailable, HttpStatusCode deductStatus, object? deductBody = null)
    {
        var handler = new FakeHttpMessageHandler(checkStatus, stockAvailable, deductStatus, deductBody);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5100") };
        return new InventoryHttpClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateInventoryClient(HttpStatusCode.OK, true, HttpStatusCode.OK);
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryServiceToDeductStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var deductResponse = new InventoryItem
        {
            Id = 1,
            ProductId = product.Id,
            ProductName = product.Name,
            QuantityOnHand = 45,
            ReorderLevel = 10,
            WarehouseLocation = "A-01"
        };
        var inventoryClient = CreateInventoryClient(HttpStatusCode.OK, true, HttpStatusCode.OK, deductResponse);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = CreateInventoryClient(HttpStatusCode.OK, false, HttpStatusCode.Conflict);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _checkStatus;
    private readonly bool _stockAvailable;
    private readonly HttpStatusCode _deductStatus;
    private readonly object? _deductBody;

    public FakeHttpMessageHandler(HttpStatusCode checkStatus, bool stockAvailable, HttpStatusCode deductStatus, object? deductBody = null)
    {
        _checkStatus = checkStatus;
        _stockAvailable = stockAvailable;
        _deductStatus = deductStatus;
        _deductBody = deductBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/check"))
        {
            return Task.FromResult(new HttpResponseMessage(_checkStatus)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { productId = 1, quantity = 5, available = _stockAvailable }),
                    System.Text.Encoding.UTF8, "application/json")
            });
        }

        if (path.Contains("/deduct"))
        {
            var response = new HttpResponseMessage(_deductStatus);
            if (_deductBody != null)
            {
                response.Content = new StringContent(
                    JsonSerializer.Serialize(_deductBody),
                    System.Text.Encoding.UTF8, "application/json");
            }
            else if (_deductStatus == HttpStatusCode.Conflict)
            {
                response.Content = new StringContent(
                    JsonSerializer.Serialize(new { error = "Insufficient stock" }),
                    System.Text.Encoding.UTF8, "application/json");
            }
            return Task.FromResult(response);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
        });
    }
}
