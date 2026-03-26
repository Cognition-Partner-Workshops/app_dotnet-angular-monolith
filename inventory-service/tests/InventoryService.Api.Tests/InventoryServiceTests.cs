using Microsoft.EntityFrameworkCore;
using Xunit;
using InventoryService.Api.Data;
using InventoryService.Api.Models;
using InventoryService.Api.Services;

namespace InventoryService.Api.Tests;

public class InventoryServiceTests
{
    private static InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new InventoryDbContext(options);
        context.InventoryItems.Add(new InventoryItem
        {
            Id = 1, ProductId = 1, ProductName = "Widget A",
            QuantityOnHand = 50, ReorderLevel = 10, WarehouseLocation = "A-01"
        });
        context.InventoryItems.Add(new InventoryItem
        {
            Id = 2, ProductId = 2, ProductName = "Widget B",
            QuantityOnHand = 5, ReorderLevel = 10, WarehouseLocation = "A-02"
        });
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task GetAllInventory_ReturnsAllItems()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var items = await service.GetAllInventoryAsync();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetByProductId_ReturnsCorrectItem()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var item = await service.GetInventoryByProductIdAsync(1);
        Assert.NotNull(item);
        Assert.Equal("Widget A", item.ProductName);
    }

    [Fact]
    public async Task Restock_IncreasesQuantity()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var item = await service.RestockAsync(1, 25);
        Assert.Equal(75, item.QuantityOnHand);
    }

    [Fact]
    public async Task GetLowStock_ReturnsLowStockItems()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var items = await service.GetLowStockItemsAsync();
        Assert.Single(items);
        Assert.Equal("Widget B", items[0].ProductName);
    }

    [Fact]
    public async Task CheckStock_ReturnsTrueWhenSufficient()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var result = await service.CheckStockAsync(1, 10);
        Assert.True(result);
    }

    [Fact]
    public async Task CheckStock_ReturnsFalseWhenInsufficient()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var result = await service.CheckStockAsync(2, 20);
        Assert.False(result);
    }

    [Fact]
    public async Task DeductStock_ReducesQuantity()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var item = await service.DeductStockAsync(1, 10);
        Assert.NotNull(item);
        Assert.Equal(40, item.QuantityOnHand);
    }

    [Fact]
    public async Task DeductStock_ReturnsNullWhenInsufficient()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var item = await service.DeductStockAsync(2, 20);
        Assert.Null(item);
    }
}
