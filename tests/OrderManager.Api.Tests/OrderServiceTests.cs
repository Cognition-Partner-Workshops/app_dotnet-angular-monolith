using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// Fake HTTP handler that simulates inventory-service responses for testing.
/// </summary>
public class FakeInventoryHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<int, InventoryItem> _inventory;

    public FakeInventoryHttpHandler()
    {
        _inventory = new Dictionary<int, InventoryItem>
        {
            { 1, new InventoryItem { Id = 1, ProductId = 1, ProductName = "Widget A", QuantityOnHand = 50, ReorderLevel = 10, WarehouseLocation = "A-01" } },
            { 2, new InventoryItem { Id = 2, ProductId = 2, ProductName = "Widget B", QuantityOnHand = 100, ReorderLevel = 10, WarehouseLocation = "A-02" } },
            { 3, new InventoryItem { Id = 3, ProductId = 3, ProductName = "Gadget X", QuantityOnHand = 150, ReorderLevel = 10, WarehouseLocation = "B-01" } },
            { 4, new InventoryItem { Id = 4, ProductId = 4, ProductName = "Gadget Y", QuantityOnHand = 200, ReorderLevel = 10, WarehouseLocation = "B-02" } },
            { 5, new InventoryItem { Id = 5, ProductId = 5, ProductName = "Gizmo Z", QuantityOnHand = 250, ReorderLevel = 10, WarehouseLocation = "C-01" } },
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/deduct"))
        {
            var segments = path.Split('/');
            var productIdStr = segments[^2];
            if (int.TryParse(productIdStr, out var pid) && _inventory.TryGetValue(pid, out var item))
            {
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(body);
                var qty = doc.RootElement.GetProperty("quantity").GetInt32();

                if (item.QuantityOnHand < qty)
                {
                    return new HttpResponseMessage(HttpStatusCode.Conflict)
                    {
                        Content = new StringContent($"{{\"error\":\"Insufficient stock for product {pid}\"}}", System.Text.Encoding.UTF8, "application/json")
                    };
                }
                item.QuantityOnHand -= qty;
                var json = JsonSerializer.Serialize(item);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
        };
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

    private static InventoryHttpClient CreateInventoryClient(HttpMessageHandler? handler = null)
    {
        handler ??= new FakeInventoryHttpHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5100") };
        return new InventoryHttpClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var service = new OrderService(context, CreateInventoryClient());
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var service = new OrderService(context, CreateInventoryClient());
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var service = new OrderService(context, CreateInventoryClient());
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

internal class FakeInventoryHandler : HttpMessageHandler
{
    private readonly bool _stockAvailable;

    public FakeInventoryHandler(bool stockAvailable = true)
    {
        _stockAvailable = stockAvailable;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("check-stock"))
        {
            var json = JsonSerializer.Serialize(new { productId = 1, quantity = 1, available = _stockAvailable });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        if (path.Contains("deduct"))
        {
            if (!_stockAvailable)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent("{\"error\":\"Insufficient stock\"}", System.Text.Encoding.UTF8, "application/json")
                });
            }

            var json = JsonSerializer.Serialize(new { id = 1, productId = 1, productName = "Widget A", quantityOnHand = 45, reorderLevel = 10, warehouseLocation = "A-01", lastRestocked = DateTime.UtcNow });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
        });
    }
}
