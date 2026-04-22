using Microsoft.EntityFrameworkCore;
using Xunit;
using InventoryService.Api.Data;
using InventoryService.Api.Models;
using InventoryService.Api.Services;

namespace InventoryService.Api.Tests;

public class InventoryItemServiceTests
{
    private InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new InventoryDbContext(options);
        SeedData.Initialize(context);
        return context;
    }

    [Fact]
    public async Task GetAllInventory_ReturnsSeedData()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var items = await service.GetAllInventoryAsync();
        Assert.Equal(5, items.Count);
    }

    [Fact]
    public async Task GetInventoryByProductId_ReturnsCorrectItem()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var item = await service.GetInventoryByProductIdAsync(1);
        Assert.NotNull(item);
        Assert.Equal("Widget A", item.ProductName);
        Assert.Equal(50, item.QuantityOnHand);
    }

    [Fact]
    public async Task GetInventoryByProductId_ReturnsNull_WhenNotFound()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var item = await service.GetInventoryByProductIdAsync(999);
        Assert.Null(item);
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
    public async Task Restock_ThrowsForInvalidProduct()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        await Assert.ThrowsAsync<ArgumentException>(() => service.RestockAsync(999, 10));
    }

    [Fact]
    public async Task GetLowStockItems_ReturnsEmpty_WhenAllAboveReorderLevel()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var items = await service.GetLowStockItemsAsync();
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetLowStockItems_ReturnsItems_WhenBelowReorderLevel()
    {
        using var context = CreateContext();
        var item = await context.InventoryItems.FirstAsync(i => i.ProductId == 1);
        item.QuantityOnHand = 5;
        await context.SaveChangesAsync();

        var service = new InventoryItemService(context);
        var lowStock = await service.GetLowStockItemsAsync();
        Assert.Single(lowStock);
        Assert.Equal(1, lowStock[0].ProductId);
    }

    [Fact]
    public async Task CheckAndDeductStock_Success()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var result = await service.CheckAndDeductStockAsync(1, 10);
        Assert.True(result.Success);
        Assert.Equal(40, result.RemainingStock);
    }

    [Fact]
    public async Task CheckAndDeductStock_FailsOnInsufficientStock()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var result = await service.CheckAndDeductStockAsync(1, 999);
        Assert.False(result.Success);
        Assert.Contains("Insufficient stock", result.Error);
    }

    [Fact]
    public async Task CheckAndDeductStock_FailsForMissingProduct()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var result = await service.CheckAndDeductStockAsync(999, 10);
        Assert.False(result.Success);
        Assert.Contains("No inventory record", result.Error);
    }

    [Fact]
    public async Task GetStockLevel_ReturnsLevel()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var level = await service.GetStockLevelAsync(1);
        Assert.Equal(50, level);
    }

    [Fact]
    public async Task GetStockLevel_ReturnsNull_WhenNotFound()
    {
        using var context = CreateContext();
        var service = new InventoryItemService(context);
        var level = await service.GetStockLevelAsync(999);
        Assert.Null(level);
    }
}
