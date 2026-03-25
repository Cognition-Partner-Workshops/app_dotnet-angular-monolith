using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

/// <summary>
/// In-memory fake of the InventoryService HTTP client for unit testing.
/// Overrides virtual methods to avoid real HTTP calls.
/// </summary>
public class FakeInventoryService : InventoryService
{
    private readonly Dictionary<int, InventoryItemDto> _inventory = new()
    {
        [1] = new InventoryItemDto { Id = 1, ProductId = 1, ProductName = "Widget A", QuantityOnHand = 50, ReorderLevel = 10, WarehouseLocation = "A-01" },
        [2] = new InventoryItemDto { Id = 2, ProductId = 2, ProductName = "Widget B", QuantityOnHand = 100, ReorderLevel = 10, WarehouseLocation = "A-02" },
        [3] = new InventoryItemDto { Id = 3, ProductId = 3, ProductName = "Gadget X", QuantityOnHand = 150, ReorderLevel = 10, WarehouseLocation = "A-03" },
        [4] = new InventoryItemDto { Id = 4, ProductId = 4, ProductName = "Gadget Y", QuantityOnHand = 200, ReorderLevel = 10, WarehouseLocation = "A-04" },
        [5] = new InventoryItemDto { Id = 5, ProductId = 5, ProductName = "Thingamajig", QuantityOnHand = 250, ReorderLevel = 10, WarehouseLocation = "A-05" },
    };

    public FakeInventoryService() : base(new HttpClient()) { }

    public override Task<List<InventoryItemDto>> GetAllInventoryAsync()
        => Task.FromResult(_inventory.Values.ToList());

    public override Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
        => Task.FromResult(_inventory.TryGetValue(productId, out var item) ? item : null);

    public override Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        if (!_inventory.TryGetValue(productId, out var item))
            throw new ArgumentException($"Product {productId} not found");
        item.QuantityOnHand += quantity;
        item.LastRestocked = DateTime.UtcNow;
        return Task.FromResult(item);
    }

    public override Task<List<InventoryItemDto>> GetLowStockItemsAsync()
        => Task.FromResult(_inventory.Values.Where(i => i.QuantityOnHand <= i.ReorderLevel).ToList());

    public override Task<bool> CheckStockAsync(int productId, int quantity)
    {
        if (_inventory.TryGetValue(productId, out var item))
            return Task.FromResult(item.QuantityOnHand >= quantity);
        return Task.FromResult(false);
    }

    public override Task<InventoryItemDto> DeductStockAsync(int productId, int quantity)
    {
        if (!_inventory.TryGetValue(productId, out var item))
            throw new InvalidOperationException($"No inventory record for product {productId}");
        if (item.QuantityOnHand < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        item.QuantityOnHand -= quantity;
        return Task.FromResult(item);
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
        var inventoryService = new FakeInventoryService();
        var service = new OrderService(context, inventoryService);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsInventoryViaService()
    {
        using var context = CreateContext();
        var inventoryService = new FakeInventoryService();
        var service = new OrderService(context, inventoryService);
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
        var inventoryService = new FakeInventoryService();
        var service = new OrderService(context, inventoryService);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
