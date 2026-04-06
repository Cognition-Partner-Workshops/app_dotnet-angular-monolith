using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class FakeInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, InventoryItemDto> _items = new();

    public FakeInventoryServiceClient(AppDbContext context)
    {
        foreach (var item in context.InventoryItems.Include(i => i.Product).ToList())
        {
            _items[item.ProductId] = new InventoryItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? string.Empty,
                QuantityOnHand = item.QuantityOnHand,
                ReorderLevel = item.ReorderLevel,
                WarehouseLocation = item.WarehouseLocation,
                LastRestocked = item.LastRestocked
            };
        }
    }

    public Task<List<InventoryItemDto>> GetAllInventoryAsync() =>
        Task.FromResult(_items.Values.ToList());

    public Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_items.GetValueOrDefault(productId));

    public Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        var item = _items[productId];
        item.QuantityOnHand += quantity;
        item.LastRestocked = DateTime.UtcNow;
        return Task.FromResult(item);
    }

    public Task<InventoryItemDto> DeductAsync(int productId, int quantity)
    {
        var item = _items.GetValueOrDefault(productId)
            ?? throw new ArgumentException($"No inventory record for product {productId}");
        if (item.QuantityOnHand < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}. Available: {item.QuantityOnHand}");
        item.QuantityOnHand -= quantity;
        return Task.FromResult(item);
    }

    public Task<List<InventoryItemDto>> GetLowStockItemsAsync() =>
        Task.FromResult(_items.Values.Where(i => i.QuantityOnHand <= i.ReorderLevel).ToList());

    public int GetQuantity(int productId) => _items[productId].QuantityOnHand;
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
        var inventoryClient = new FakeInventoryServiceClient(context);
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsInventory()
    {
        using var context = CreateContext();
        var inventoryClient = new FakeInventoryServiceClient(context);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var qtyBefore = inventoryClient.GetQuantity(product.Id);

        await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.Equal(qtyBefore - 5, inventoryClient.GetQuantity(product.Id));
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = new FakeInventoryServiceClient(context);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
