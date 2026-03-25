using System.Net;
using System.Net.Http.Json;
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

    private static InventoryHttpClient CreateInventoryClient(
        Dictionary<int, int>? stockLevels = null, bool deductSucceeds = true)
    {
        var handler = new FakeInventoryHandler(stockLevels ?? new Dictionary<int, int>(), deductSucceeds);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://fake-inventory/")
        };
        return new InventoryHttpClient(httpClient);
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
        var stockLevels = new Dictionary<int, int> { { product.Id, 100 } };
        var inventoryClient = CreateInventoryClient(stockLevels);
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
        var stockLevels = new Dictionary<int, int> { { product.Id, 2 } };
        var inventoryClient = CreateInventoryClient(stockLevels);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

public class FakeInventoryHandler : HttpMessageHandler
{
    private readonly Dictionary<int, int> _stockLevels;
    private readonly bool _deductSucceeds;

    public FakeInventoryHandler(Dictionary<int, int> stockLevels, bool deductSucceeds = true)
    {
        _stockLevels = stockLevels;
        _deductSucceeds = deductSucceeds;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (request.Method == HttpMethod.Get && path.Contains("/api/inventory/product/"))
        {
            var segments = path.Split('/');
            var productIdStr = segments[^1];
            if (int.TryParse(productIdStr, out var productId) && _stockLevels.TryGetValue(productId, out var qty))
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new InventoryItemDto
                    {
                        Id = productId,
                        ProductId = productId,
                        ProductName = $"Product {productId}",
                        QuantityOnHand = qty,
                        ReorderLevel = 10,
                        WarehouseLocation = "A-01",
                        LastRestocked = DateTime.UtcNow
                    })
                };
                return Task.FromResult(response);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        if (request.Method == HttpMethod.Post && path.Contains("/deduct"))
        {
            if (_deductSucceeds)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new InventoryItemDto
                    {
                        Id = 1, ProductId = 1, ProductName = "Widget A",
                        QuantityOnHand = 45, ReorderLevel = 10,
                        WarehouseLocation = "A-01", LastRestocked = DateTime.UtcNow
                    })
                };
                return Task.FromResult(response);
            }
            else
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = JsonContent.Create(new { error = "Insufficient stock" })
                });
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
