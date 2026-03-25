using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// Fake inventory client that delegates to the in-memory DbContext for testing.
/// This simulates the inventory microservice behavior using the local database.
/// </summary>
public class FakeInventoryServiceClient : IInventoryServiceClient
{
    private readonly AppDbContext _context;

    public FakeInventoryServiceClient(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryItemDto>> GetAllInventoryAsync()
    {
        var items = await _context.InventoryItems.Include(i => i.Product).ToListAsync();
        return items.Select(MapToDto).ToList();
    }

    public async Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        var item = await _context.InventoryItems.Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId);
        return item is null ? null : MapToDto(item);
    }

    public async Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        var item = await _context.InventoryItems.Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId)
            ?? throw new ArgumentException($"No inventory record for product {productId}");
        item.QuantityOnHand += quantity;
        item.LastRestocked = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task<InventoryItemDto> DeductStockAsync(int productId, int quantity)
    {
        var item = await _context.InventoryItems.Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId)
            ?? throw new ArgumentException($"No inventory record for product {productId}");

        if (item.QuantityOnHand < quantity)
            throw new InvalidOperationException(
                $"Insufficient stock for {item.Product.Name}. Available: {item.QuantityOnHand}");

        item.QuantityOnHand -= quantity;
        await _context.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        var items = await _context.InventoryItems.Include(i => i.Product)
            .Where(i => i.QuantityOnHand <= i.ReorderLevel)
            .ToListAsync();
        return items.Select(MapToDto).ToList();
    }

    private static InventoryItemDto MapToDto(InventoryItem item) => new()
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
        var inventoryBefore = await context.InventoryItems.FirstAsync(i => i.ProductId == product.Id);
        var qtyBefore = inventoryBefore.QuantityOnHand;

        await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        var inventoryAfter = await context.InventoryItems.FirstAsync(i => i.ProductId == product.Id);
        Assert.Equal(qtyBefore - 5, inventoryAfter.QuantityOnHand);
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
