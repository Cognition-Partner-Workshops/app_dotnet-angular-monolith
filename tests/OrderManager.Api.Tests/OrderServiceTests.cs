using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

/// <summary>
/// A test-only HTTP message handler that returns canned responses
/// for inventory microservice calls.
/// </summary>
internal class FakeInventoryHandler : HttpMessageHandler
{
    private readonly int _availableStock;

    public FakeInventoryHandler(int availableStock = 100)
    {
        _availableStock = availableStock;
    }

    public List<(string Method, string Url)> Calls { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls.Add((request.Method.ToString(), request.RequestUri!.ToString()));

        var uri = request.RequestUri!.AbsolutePath;

        // GET api/inventory/product/{id}/check?quantity=N
        if (uri.Contains("/check") && request.Method == HttpMethod.Get)
        {
            var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
            var qty = int.Parse(query["quantity"] ?? "0");
            var inStock = qty <= _availableStock;
            var response = new StockCheckResponse(1, _availableStock, inStock);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });
        }

        // POST api/inventory/product/{id}/deduct
        if (uri.Contains("/deduct") && request.Method == HttpMethod.Post)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { })
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

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

    private static InventoryServiceClient CreateInventoryClient(int availableStock = 100)
    {
        var handler = new FakeInventoryHandler(availableStock);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://fake-inventory") };
        return new InventoryServiceClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryServiceToCheckAndDeductStock()
    {
        using var context = CreateContext();
        var handler = new FakeInventoryHandler(availableStock: 100);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://fake-inventory") };
        var inventoryClient = new InventoryServiceClient(httpClient);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        // Verify the inventory service was called for stock check and deduction
        Assert.Contains(handler.Calls, c => c.Method == "GET" && c.Url.Contains("/check"));
        Assert.Contains(handler.Calls, c => c.Method == "POST" && c.Url.Contains("/deduct"));
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = CreateInventoryClient(availableStock: 0);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
