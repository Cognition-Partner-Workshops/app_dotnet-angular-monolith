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
/// In-memory mock of the inventory microservice client.
/// Since inventory data is no longer in AppDbContext, this mock
/// maintains its own dictionary of inventory items.
/// </summary>
public class MockInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, InventoryItem> _inventory = new()
    {
        [1] = new InventoryItem { Id = 1, ProductId = 1, ProductName = "Widget A", QuantityOnHand = 50, ReorderLevel = 10, WarehouseLocation = "A-01" },
        [2] = new InventoryItem { Id = 2, ProductId = 2, ProductName = "Widget B", QuantityOnHand = 100, ReorderLevel = 10, WarehouseLocation = "A-02" },
        [3] = new InventoryItem { Id = 3, ProductId = 3, ProductName = "Gadget X", QuantityOnHand = 150, ReorderLevel = 10, WarehouseLocation = "A-03" },
        [4] = new InventoryItem { Id = 4, ProductId = 4, ProductName = "Gadget Y", QuantityOnHand = 200, ReorderLevel = 10, WarehouseLocation = "A-04" },
        [5] = new InventoryItem { Id = 5, ProductId = 5, ProductName = "Thingamajig", QuantityOnHand = 250, ReorderLevel = 10, WarehouseLocation = "A-05" },
    };

    public Task<List<InventoryItem>> GetAllInventoryAsync()
        => Task.FromResult(_inventory.Values.ToList());

    public Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
        => Task.FromResult(_inventory.TryGetValue(productId, out var item) ? item : null);

    public Task<InventoryItem> RestockAsync(int productId, int quantity)
    {
        if (!_inventory.TryGetValue(productId, out var item))
            throw new ArgumentException($"Product {productId} not found");
        item.QuantityOnHand += quantity;
        item.LastRestocked = DateTime.UtcNow;
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
        if (_inventory.TryGetValue(productId, out var item))
            return Task.FromResult(item.QuantityOnHand);
        return Task.FromResult(0);
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

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
