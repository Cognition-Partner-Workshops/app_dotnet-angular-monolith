using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderManager.Api.Clients;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

/// <summary>
/// Test implementation of IInventoryServiceClient that simulates the inventory microservice.
/// </summary>
public class FakeInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, InventoryItemDto> _items = new()
    {
        [1] = new InventoryItemDto { Id = 1, ProductId = 1, ProductName = "Widget A", QuantityOnHand = 50, ReorderLevel = 10, WarehouseLocation = "A-01" },
        [2] = new InventoryItemDto { Id = 2, ProductId = 2, ProductName = "Widget B", QuantityOnHand = 100, ReorderLevel = 10, WarehouseLocation = "A-02" },
        [3] = new InventoryItemDto { Id = 3, ProductId = 3, ProductName = "Gadget X", QuantityOnHand = 150, ReorderLevel = 10, WarehouseLocation = "A-03" },
        [4] = new InventoryItemDto { Id = 4, ProductId = 4, ProductName = "Gadget Y", QuantityOnHand = 200, ReorderLevel = 10, WarehouseLocation = "A-04" },
        [5] = new InventoryItemDto { Id = 5, ProductId = 5, ProductName = "Thingamajig", QuantityOnHand = 250, ReorderLevel = 10, WarehouseLocation = "A-05" },
    };

    public Task<List<InventoryItemDto>> GetAllInventoryAsync() =>
        Task.FromResult(_items.Values.ToList());

    public Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_items.GetValueOrDefault(productId));

    public Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        if (!_items.TryGetValue(productId, out var item))
            throw new ArgumentException($"No inventory record for product {productId}");
        item.QuantityOnHand += quantity;
        return Task.FromResult(item);
    }

    public Task<List<InventoryItemDto>> GetLowStockItemsAsync() =>
        Task.FromResult(_items.Values.Where(i => i.QuantityOnHand <= i.ReorderLevel).ToList());

    public Task<bool> CheckStockAsync(int productId, int quantity) =>
        Task.FromResult(_items.TryGetValue(productId, out var item) && item.QuantityOnHand >= quantity);

    public Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        if (!_items.TryGetValue(productId, out var item) || item.QuantityOnHand < quantity)
            return Task.FromResult<InventoryItemDto?>(null);
        item.QuantityOnHand -= quantity;
        return Task.FromResult<InventoryItemDto?>(item);
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
        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
