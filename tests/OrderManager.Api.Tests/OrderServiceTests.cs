using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

/// <summary>
/// Unit tests for <see cref="OrderService"/> using an in-memory database.
/// </summary>
public class OrderServiceTests
{
    /// <summary>
    /// Creates a fresh in-memory database context pre-populated with seed data.
    /// Each test gets an isolated database instance (unique name via GUID).
    /// </summary>
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        SeedData.Initialize(context);
        return context;
    }

    /// <summary>
    /// Verifies that GetAllOrdersAsync returns an empty list when no orders have been placed
    /// (seed data only creates customers, products, and inventory—not orders).
    /// </summary>
    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var service = new OrderService(context);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    /// <summary>
    /// Verifies that creating an order reduces the on-hand inventory by the ordered quantity.
    /// </summary>
    [Fact]
    public async Task CreateOrder_DeductsInventory()
    {
        using var context = CreateContext();
        var service = new OrderService(context);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventoryBefore = await context.InventoryItems.FirstAsync(i => i.ProductId == product.Id);
        var qtyBefore = inventoryBefore.QuantityOnHand;

        await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        var inventoryAfter = await context.InventoryItems.FirstAsync(i => i.ProductId == product.Id);
        Assert.Equal(qtyBefore - 5, inventoryAfter.QuantityOnHand);
    }

    /// <summary>
    /// Verifies that ordering more units than available throws an
    /// <see cref="InvalidOperationException"/> for insufficient stock.
    /// </summary>
    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var service = new OrderService(context);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
