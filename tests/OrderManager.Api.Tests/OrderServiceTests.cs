using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// Mock inventory service client that delegates to the local database
/// for testing the OrderService after the inventory microservice extraction.
/// </summary>
public class MockInventoryServiceClient : IInventoryServiceClient
{
    private readonly AppDbContext _context;

    public MockInventoryServiceClient(AppDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryStockLevel?> GetStockLevelAsync(int productId)
    {
        var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
        if (item == null) return null;
        return new InventoryStockLevel(item.ProductId, item.QuantityOnHand);
    }

    public async Task<bool> DeductStockAsync(int productId, int quantity)
    {
        var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
        if (item == null || item.QuantityOnHand < quantity) return false;
        item.QuantityOnHand -= quantity;
        await _context.SaveChangesAsync();
        return true;
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
        var inventoryClient = new MockInventoryServiceClient(context);
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsInventory()
    {
        using var context = CreateContext();
        var inventoryClient = new MockInventoryServiceClient(context);
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
        var inventoryClient = new MockInventoryServiceClient(context);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
