using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

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
        var service = new OrderService(context);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

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

    [Fact]
    public async Task GetOrderByIdAsync_ReturnsOrderWithCustomerAndItems()
    {
        using var context = CreateContext();
        var service = new OrderService(context);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var created = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 2) });
        var fetched = await service.GetOrderByIdAsync(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.NotNull(fetched.Customer);
        Assert.Equal(customer.Id, fetched.Customer.Id);
        Assert.NotEmpty(fetched.Items);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ReturnsNull_ForNonExistentId()
    {
        using var context = CreateContext();
        var service = new OrderService(context);

        var result = await service.GetOrderByIdAsync(99999);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_UpdatesStatusAndReturnsOrder()
    {
        using var context = CreateContext();
        var service = new OrderService(context);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 1) });
        var updated = await service.UpdateOrderStatusAsync(order.Id, "Shipped");

        Assert.Equal("Shipped", updated.Status);
        Assert.Equal(order.Id, updated.Id);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ThrowsArgumentException_ForNonExistentOrder()
    {
        using var context = CreateContext();
        var service = new OrderService(context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateOrderStatusAsync(99999, "Shipped"));
    }

    [Fact]
    public async Task CreateOrder_ThrowsArgumentException_ForNonExistentCustomer()
    {
        using var context = CreateContext();
        var service = new OrderService(context);
        var product = await context.Products.FirstAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateOrderAsync(99999, new List<(int, int)> { (product.Id, 1) }));
    }

    [Fact]
    public async Task CreateOrder_ThrowsArgumentException_ForNonExistentProduct()
    {
        using var context = CreateContext();
        var service = new OrderService(context);
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (99999, 1) }));
    }
}
