using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// A fake HTTP message handler that simulates the inventory microservice responses.
/// </summary>
public class FakeInventoryHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<int, InventoryItem> _inventory = new()
    {
        [1] = new InventoryItem { Id = 1, ProductId = 1, ProductName = "Widget A", ProductSku = "WGT-001", QuantityOnHand = 50, ReorderLevel = 10, WarehouseLocation = "A-01" },
        [2] = new InventoryItem { Id = 2, ProductId = 2, ProductName = "Widget B", ProductSku = "WGT-002", QuantityOnHand = 100, ReorderLevel = 10, WarehouseLocation = "A-02" },
        [3] = new InventoryItem { Id = 3, ProductId = 3, ProductName = "Gadget X", ProductSku = "GDG-001", QuantityOnHand = 150, ReorderLevel = 10, WarehouseLocation = "A-03" },
        [4] = new InventoryItem { Id = 4, ProductId = 4, ProductName = "Gadget Y", ProductSku = "GDG-002", QuantityOnHand = 200, ReorderLevel = 10, WarehouseLocation = "A-04" },
        [5] = new InventoryItem { Id = 5, ProductId = 5, ProductName = "Thingamajig", ProductSku = "THG-001", QuantityOnHand = 250, ReorderLevel = 10, WarehouseLocation = "A-05" },
    };

    public bool InsufficientStockMode { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        // POST /api/inventory/product/{id}/deduct
        if (request.Method == HttpMethod.Post && path.Contains("/deduct"))
        {
            var segments = path.Split('/');
            var productIdStr = segments[^2]; // product id is second to last before "deduct"
            if (int.TryParse(productIdStr, out var productId) && _inventory.TryGetValue(productId, out var item))
            {
                if (InsufficientStockMode)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
                    {
                        Content = JsonContent.Create(new { Error = "Insufficient stock" })
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(item)
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = JsonContent.Create(new { Error = $"No inventory record for product {productIdStr}" })
            });
        }

        // GET /api/inventory
        if (request.Method == HttpMethod.Get && path == "/api/inventory")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(_inventory.Values.ToList())
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

    private static InventoryHttpClient CreateInventoryClient(FakeInventoryHttpHandler? handler = null)
    {
        handler ??= new FakeInventoryHttpHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5001") };
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
    public async Task CreateOrder_CallsInventoryServiceToDeductStock()
    {
        using var context = CreateContext();
        var inventoryClient = CreateInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var handler = new FakeInventoryHttpHandler { InsufficientStockMode = true };
        var inventoryClient = CreateInventoryClient(handler);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) }));
    }
}
