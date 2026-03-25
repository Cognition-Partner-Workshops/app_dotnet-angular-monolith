using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// In-memory mock of IInventoryServiceClient for testing OrderService
/// without requiring the inventory microservice to be running.
/// </summary>
public class MockInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, InventoryItem> _inventory = new();

    public MockInventoryServiceClient(IEnumerable<InventoryItem> seedItems)
    {
        foreach (var item in seedItems)
            _inventory[item.ProductId] = item;
    }

    public Task<List<InventoryItem>> GetAllInventoryAsync()
        => Task.FromResult(_inventory.Values.ToList());

    public Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
        => Task.FromResult(_inventory.GetValueOrDefault(productId));

    public Task<InventoryItem> RestockAsync(int productId, int quantity)
    {
        if (!_inventory.TryGetValue(productId, out var item))
            throw new InvalidOperationException($"Inventory not found for product {productId}");
        item.QuantityOnHand += quantity;
        return Task.FromResult(item);
    }

    public Task<List<InventoryItem>> GetLowStockItemsAsync()
        => Task.FromResult(_inventory.Values.Where(i => i.QuantityOnHand <= i.ReorderLevel).ToList());

    public Task<InventoryItem?> DeductStockAsync(int productId, int quantity)
    {
        if (!_inventory.TryGetValue(productId, out var item))
            return Task.FromResult<InventoryItem?>(null);
        if (item.QuantityOnHand < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        item.QuantityOnHand -= quantity;
        return Task.FromResult<InventoryItem?>(item);
    }

    public Task<int> GetStockLevelAsync(int productId)
    {
        var qty = _inventory.TryGetValue(productId, out var item) ? item.QuantityOnHand : 0;
        return Task.FromResult(qty);
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

    private static MockInventoryServiceClient CreateMockInventoryClient()
    {
        return new MockInventoryServiceClient(new[]
        {
            new InventoryItem { Id = 1, ProductId = 1, ProductName = "Widget A", QuantityOnHand = 50, ReorderLevel = 10, WarehouseLocation = "A-01" },
            new InventoryItem { Id = 2, ProductId = 2, ProductName = "Widget B", QuantityOnHand = 100, ReorderLevel = 10, WarehouseLocation = "A-02" },
            new InventoryItem { Id = 3, ProductId = 3, ProductName = "Gadget X", QuantityOnHand = 150, ReorderLevel = 10, WarehouseLocation = "B-01" },
            new InventoryItem { Id = 4, ProductId = 4, ProductName = "Gadget Y", QuantityOnHand = 200, ReorderLevel = 10, WarehouseLocation = "B-02" },
            new InventoryItem { Id = 5, ProductId = 5, ProductName = "Gizmo Z", QuantityOnHand = 250, ReorderLevel = 10, WarehouseLocation = "C-01" },
        });
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient();
        var service = new OrderService(context, inventoryClient);
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
        var inventoryClient = CreateMockInventoryClient();
        var service = new OrderService(context, inventoryClient);
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
