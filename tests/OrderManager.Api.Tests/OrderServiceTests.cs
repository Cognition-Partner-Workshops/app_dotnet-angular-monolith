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
/// Fake HTTP message handler to simulate inventory microservice responses.
/// </summary>
public class FakeInventoryHandler : HttpMessageHandler
{
    private readonly Dictionary<int, int> _stockLevels = new();

    public FakeInventoryHandler(Dictionary<int, int> stockLevels)
    {
        _stockLevels = stockLevels;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;

        // Handle deduct stock: POST /api/inventory/product/{id}/deduct
        if (request.Method == HttpMethod.Post && path.Contains("/deduct"))
        {
            var segments = path.Split('/');
            var productIdStr = segments[^2]; // second to last segment
            if (int.TryParse(productIdStr, out var productId) && _stockLevels.ContainsKey(productId))
            {
                var content = request.Content!.ReadAsStringAsync(cancellationToken).Result;
                var doc = JsonDocument.Parse(content);
                var qty = doc.RootElement.GetProperty("quantity").GetInt32();

                if (_stockLevels[productId] < qty)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
                    {
                        Content = JsonContent.Create(new { error = $"Insufficient stock for product {productId}. Available: {_stockLevels[productId]}" })
                    });
                }

                _stockLevels[productId] -= qty;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new InventoryItemDto
                    {
                        ProductId = productId,
                        QuantityOnHand = _stockLevels[productId]
                    })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
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

    private InventoryHttpClient CreateInventoryClient(Dictionary<int, int> stockLevels)
    {
        var handler = new FakeInventoryHandler(stockLevels);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5002") };
        return new InventoryHttpClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateInventoryClient(new Dictionary<int, int>());
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsInventoryViaHttpClient()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var stockLevels = new Dictionary<int, int> { { product.Id, 50 } };
        var inventoryClient = CreateInventoryClient(stockLevels);
        var service = new OrderService(context, inventoryClient);

        await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.Equal(45, stockLevels[product.Id]);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var stockLevels = new Dictionary<int, int> { { product.Id, 3 } };
        var inventoryClient = CreateInventoryClient(stockLevels);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
