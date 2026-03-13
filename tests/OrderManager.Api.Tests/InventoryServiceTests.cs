using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class InventoryServiceTests
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
    public async Task GetAllInventoryAsync_ReturnsAllItems()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);

        var items = await service.GetAllInventoryAsync();

        Assert.Equal(5, items.Count);
    }

    [Fact]
    public async Task GetAllInventoryAsync_IncludesProduct()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);

        var items = await service.GetAllInventoryAsync();

        Assert.All(items, item => Assert.NotNull(item.Product));
    }

    [Fact]
    public async Task GetInventoryByProductIdAsync_ReturnsItemForExistingProduct()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);
        var product = await context.Products.FirstAsync();

        var item = await service.GetInventoryByProductIdAsync(product.Id);

        Assert.NotNull(item);
        Assert.Equal(product.Id, item.ProductId);
    }

    [Fact]
    public async Task GetInventoryByProductIdAsync_IncludesProduct()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);
        var product = await context.Products.FirstAsync();

        var item = await service.GetInventoryByProductIdAsync(product.Id);

        Assert.NotNull(item);
        Assert.NotNull(item.Product);
        Assert.Equal(product.Name, item.Product.Name);
    }

    [Fact]
    public async Task GetInventoryByProductIdAsync_ReturnsNullForNonExistentProduct()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);

        var item = await service.GetInventoryByProductIdAsync(9999);

        Assert.Null(item);
    }

    [Fact]
    public async Task RestockAsync_IncreasesQuantityOnHand()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);
        var product = await context.Products.FirstAsync();
        var inventoryBefore = await context.InventoryItems.FirstAsync(i => i.ProductId == product.Id);
        var qtyBefore = inventoryBefore.QuantityOnHand;

        var result = await service.RestockAsync(product.Id, 25);

        Assert.Equal(qtyBefore + 25, result.QuantityOnHand);
    }

    [Fact]
    public async Task RestockAsync_UpdatesLastRestocked()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);
        var product = await context.Products.FirstAsync();
        var before = DateTime.UtcNow;

        var result = await service.RestockAsync(product.Id, 10);

        Assert.True(result.LastRestocked >= before);
    }

    [Fact]
    public async Task RestockAsync_ThrowsArgumentExceptionForNonExistentProduct()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RestockAsync(9999, 10));
    }

    [Fact]
    public async Task GetLowStockItemsAsync_ReturnsItemsBelowOrAtReorderLevel()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);

        // Seed data creates items with QuantityOnHand = (i+1)*50 and ReorderLevel = 10,
        // so none are low stock by default. Manually set one item to low stock.
        var item = await context.InventoryItems.FirstAsync();
        item.QuantityOnHand = 5;
        await context.SaveChangesAsync();

        var lowStock = await service.GetLowStockItemsAsync();

        Assert.Single(lowStock);
        Assert.Equal(item.Id, lowStock[0].Id);
    }

    [Fact]
    public async Task GetLowStockItemsAsync_IncludesItemAtExactReorderLevel()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);

        var item = await context.InventoryItems.FirstAsync();
        item.QuantityOnHand = item.ReorderLevel; // exactly at reorder level
        await context.SaveChangesAsync();

        var lowStock = await service.GetLowStockItemsAsync();

        Assert.Contains(lowStock, i => i.Id == item.Id);
    }

    [Fact]
    public async Task GetLowStockItemsAsync_ReturnsEmptyWhenAllStockAboveReorderLevel()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);

        // Seed data has all items well above reorder level
        var lowStock = await service.GetLowStockItemsAsync();

        Assert.Empty(lowStock);
    }

    [Fact]
    public async Task GetLowStockItemsAsync_IncludesProduct()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);

        var item = await context.InventoryItems.FirstAsync();
        item.QuantityOnHand = 1;
        await context.SaveChangesAsync();

        var lowStock = await service.GetLowStockItemsAsync();

        Assert.All(lowStock, i => Assert.NotNull(i.Product));
    }
}
