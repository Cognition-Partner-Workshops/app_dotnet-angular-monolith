using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;
using RichardSzalay.MockHttp;
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

    private static InventoryHttpClient CreateMockInventoryClient(int stockQuantity = 100)
    {
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When(HttpMethod.Get, "*/api/inventory/product/*")
            .Respond("application/json", JsonSerializer.Serialize(new InventoryItemDto
            {
                Id = 1,
                ProductId = 1,
                ProductName = "Widget A",
                QuantityOnHand = stockQuantity,
                ReorderLevel = 10,
                WarehouseLocation = "A-01",
                LastRestocked = DateTime.UtcNow
            }));

        mockHttp.When(HttpMethod.Post, "*/api/inventory/product/*/restock")
            .Respond("application/json", JsonSerializer.Serialize(new InventoryItemDto
            {
                Id = 1,
                ProductId = 1,
                ProductName = "Widget A",
                QuantityOnHand = stockQuantity - 5,
                ReorderLevel = 10,
                WarehouseLocation = "A-01",
                LastRestocked = DateTime.UtcNow
            }));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5002");
        return new InventoryHttpClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient(100);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient(2);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
