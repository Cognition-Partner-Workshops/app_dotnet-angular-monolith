using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Controllers;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class InventoryControllerTests
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
    public async Task GetAll_ReturnsOkWithList()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);
        var controller = new InventoryController(service);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsType<List<InventoryItem>>(okResult.Value);
        Assert.Equal(5, items.Count);
    }

    [Fact]
    public async Task GetByProduct_ReturnsOkForExistingProduct()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);
        var controller = new InventoryController(service);
        var product = await context.Products.FirstAsync();

        var result = await controller.GetByProduct(product.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var item = Assert.IsType<InventoryItem>(okResult.Value);
        Assert.Equal(product.Id, item.ProductId);
    }

    [Fact]
    public async Task GetByProduct_ReturnsNotFoundForMissingProduct()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);
        var controller = new InventoryController(service);

        var result = await controller.GetByProduct(9999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Restock_ReturnsOkWithRestockedItem()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);
        var controller = new InventoryController(service);
        var product = await context.Products.FirstAsync();
        var inventoryBefore = await context.InventoryItems.FirstAsync(i => i.ProductId == product.Id);
        var qtyBefore = inventoryBefore.QuantityOnHand;

        var result = await controller.Restock(product.Id, new RestockRequest(20));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var item = Assert.IsType<InventoryItem>(okResult.Value);
        Assert.Equal(qtyBefore + 20, item.QuantityOnHand);
    }

    [Fact]
    public async Task GetLowStock_ReturnsOkWithLowStockItems()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);
        var controller = new InventoryController(service);

        // Set one item to low stock
        var item = await context.InventoryItems.FirstAsync();
        item.QuantityOnHand = 3;
        await context.SaveChangesAsync();

        var result = await controller.GetLowStock();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsType<List<InventoryItem>>(okResult.Value);
        Assert.Single(items);
    }

    [Fact]
    public async Task GetLowStock_ReturnsOkWithEmptyListWhenNoLowStock()
    {
        using var context = CreateContext();
        var service = new InventoryService(context);
        var controller = new InventoryController(service);

        // Seed data has all items well above reorder level
        var result = await controller.GetLowStock();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsType<List<InventoryItem>>(okResult.Value);
        Assert.Empty(items);
    }
}
