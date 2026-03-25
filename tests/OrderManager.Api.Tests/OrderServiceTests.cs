using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// Fake HTTP message handler that simulates the inventory-service API responses.
/// Used to create an InventoryApiClient backed by in-memory stock data for testing.
/// </summary>
public class FakeInventoryHandler : HttpMessageHandler
{
    private readonly Dictionary<int, int> _stock;

    public FakeInventoryHandler(Dictionary<int, int>? initialStock = null)
    {
        _stock = initialStock ?? new Dictionary<int, int>
        {
            { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        // POST api/inventory/product/{id}/deduct — used by CheckAndDeductStockAsync
        if (path.Contains("/deduct"))
        {
            // Extract productId from path: .../product/{id}/deduct
            var segments = path.Split('/');
            int productId = 0;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "product" && i + 1 < segments.Length)
                {
                    int.TryParse(segments[i + 1], out productId);
                    break;
                }
            }

            if (!_stock.ContainsKey(productId) || _stock[productId] <= 0)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict));
            }

            _stock[productId] = Math.Max(0, _stock[productId] - 5);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        // GET api/inventory — used by GetAllInventoryAsync
        if (path.TrimEnd('/').EndsWith("api/inventory"))
        {
            var items = _stock.Select(kv => new InventoryItem
            {
                Id = kv.Key,
                ProductId = kv.Key,
                ProductName = $"Product {kv.Key}",
                QuantityOnHand = kv.Value
            }).ToList();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(items)
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

    private static InventoryApiClient CreateInventoryClient(Dictionary<int, int>? stock = null)
    {
        var handler = new FakeInventoryHandler(stock);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://fake-inventory/") };
        return new InventoryApiClient(httpClient);
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
    public async Task CreateOrder_SucceedsWhenStockAvailable()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryClient = CreateInventoryClient(new Dictionary<int, int> { { product.Id, 100 } });
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
        // Stock of 0 means the deduct endpoint will return Conflict
        var inventoryClient = CreateInventoryClient(new Dictionary<int, int> { { product.Id, 0 } });
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenProductNotInInventory()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        // Empty stock means product won't be found, deduct returns Conflict
        var inventoryClient = CreateInventoryClient(new Dictionary<int, int>());
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 1) }));
    }
}
