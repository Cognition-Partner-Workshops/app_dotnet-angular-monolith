using Microsoft.EntityFrameworkCore;
using InventoryService.Api.Data;
using InventoryService.Api.Models;
using FluentAssertions;
using Xunit;

namespace InventoryService.Api.Tests;

public class InventoryServiceTests
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
        var service = new Services.InventoryService(context);

        var items = await service.GetAllInventoryAsync();

        items.Should().HaveCount(5);
    }

    [Fact]
    public async Task ReserveStock_DeductsQuantity_WhenSufficientStock()
    {
        using var context = CreateContext();
        var service = new Services.InventoryService(context);
        var item = await context.InventoryItems.FirstAsync();
        var qtyBefore = item.QuantityOnHand;

        var (success, _) = await service.ReserveStockAsync(item.ProductId, 5);

        success.Should().BeTrue();
        var updated = await context.InventoryItems.FirstAsync(i => i.ProductId == item.ProductId);
        updated.QuantityOnHand.Should().Be(qtyBefore - 5);
    }

    [Fact]
    public async Task ReserveStock_ReturnsFail_WhenInsufficientStock()
    {
        using var context = CreateContext();
        var service = new Services.InventoryService(context);
        var item = await context.InventoryItems.FirstAsync();

        var (success, message) = await service.ReserveStockAsync(item.ProductId, 99999);

        success.Should().BeFalse();
        message.Should().Contain("Insufficient stock");
    }

    [Fact]
    public async Task ReleaseStock_RestoresQuantity()
    {
        using var context = CreateContext();
        var service = new Services.InventoryService(context);
        var item = await context.InventoryItems.FirstAsync();
        var qtyBefore = item.QuantityOnHand;

        await service.ReserveStockAsync(item.ProductId, 10);
        var (success, _) = await service.ReleaseStockAsync(item.ProductId, 10);

        success.Should().BeTrue();
        var updated = await context.InventoryItems.FirstAsync(i => i.ProductId == item.ProductId);
        updated.QuantityOnHand.Should().Be(qtyBefore);
    }

    [Fact]
    public async Task ReserveStock_ConcurrentRequests_DoNotOversell()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var setupContext = new InventoryDbContext(options))
        {
            setupContext.InventoryItems.Add(new InventoryItem
            {
                ProductId = 100,
                QuantityOnHand = 10,
                ReorderLevel = 5,
                WarehouseLocation = "T-01"
            });
            setupContext.SaveChanges();
        }

        var tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            using var context = new InventoryDbContext(options);
            var service = new Services.InventoryService(context);
            return await service.ReserveStockAsync(100, 3);
        });

        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r.Success);
        successCount.Should().BeLessOrEqualTo(3);

        using var verifyContext = new InventoryDbContext(options);
        var finalItem = await verifyContext.InventoryItems.FirstAsync(i => i.ProductId == 100);
        finalItem.QuantityOnHand.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetInventoryByProductId_ReturnsItem_WhenExists()
    {
        using var context = CreateContext();
        var service = new Services.InventoryService(context);

        var item = await service.GetInventoryByProductIdAsync(1);

        item.Should().NotBeNull();
        item!.ProductId.Should().Be(1);
    }

    [Fact]
    public async Task GetInventoryByProductId_ReturnsNull_WhenNotFound()
    {
        using var context = CreateContext();
        var service = new Services.InventoryService(context);

        var item = await service.GetInventoryByProductIdAsync(9999);

        item.Should().BeNull();
    }
}
