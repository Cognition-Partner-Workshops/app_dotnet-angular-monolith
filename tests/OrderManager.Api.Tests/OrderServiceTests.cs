using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

/// <summary>
/// In-memory mock of the inventory microservice client.
/// Since inventory data is no longer in AppDbContext, this mock
/// maintains its own dictionary of inventory items.
/// </summary>
public class MockInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, InventoryDto> _inventory = new()
    {
        [1] = new InventoryDto(1, 1, "Widget A", "WGT-001", 50, 10, "A-01", DateTime.UtcNow),
        [2] = new InventoryDto(2, 2, "Widget B", "WGT-002", 100, 10, "A-02", DateTime.UtcNow),
        [3] = new InventoryDto(3, 3, "Gadget X", "GDG-001", 150, 10, "A-03", DateTime.UtcNow),
        [4] = new InventoryDto(4, 4, "Gadget Y", "GDG-002", 200, 10, "A-04", DateTime.UtcNow),
        [5] = new InventoryDto(5, 5, "Thingamajig", "THG-001", 250, 10, "A-05", DateTime.UtcNow),
    };

    public Task<List<InventoryDto>> GetAllInventoryAsync()
        => Task.FromResult(_inventory.Values.ToList());

    public Task<InventoryDto?> GetInventoryByProductIdAsync(int productId)
        => Task.FromResult(_inventory.TryGetValue(productId, out var item) ? item : null);

    public Task<InventoryDto> RestockAsync(int productId, int quantity)
    {
        if (!_inventory.TryGetValue(productId, out var item))
            throw new ArgumentException($"Product {productId} not found");
        var updated = item with { QuantityOnHand = item.QuantityOnHand + quantity, LastRestocked = DateTime.UtcNow };
        _inventory[productId] = updated;
        return Task.FromResult(updated);
    }

    public Task<List<InventoryDto>> GetLowStockItemsAsync()
        => Task.FromResult(_inventory.Values.Where(i => i.QuantityOnHand <= i.ReorderLevel).ToList());

    public Task<InventoryDto?> DeductStockAsync(int productId, int quantity)
    {
        if (!_inventory.TryGetValue(productId, out var item))
            return Task.FromResult<InventoryDto?>(null);
        if (item.QuantityOnHand < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        var updated = item with { QuantityOnHand = item.QuantityOnHand - quantity };
        _inventory[productId] = updated;
        return Task.FromResult<InventoryDto?>(updated);
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
