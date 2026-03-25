using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

    private static InventoryHttpClient CreateInventoryClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5100") };
        var logger = LoggerFactory.Create(b => { }).CreateLogger<InventoryHttpClient>();
        return new InventoryHttpClient(httpClient, logger);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var handler = new MockInventoryHandler();
        var inventoryClient = CreateInventoryClient(handler);
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryServiceToDeductStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var handler = new MockInventoryHandler();
        var inventoryClient = CreateInventoryClient(handler);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Id, order.Items.First().ProductId);
        Assert.True(handler.DeductStockCalled, "DeductStock should be called on inventory-service");
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var handler = new MockInventoryHandler(simulateInsufficientStock: true);
        var inventoryClient = CreateInventoryClient(handler);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

/// <summary>
/// Mock HTTP handler that simulates the inventory-service responses for testing.
/// </summary>
public class MockInventoryHandler : HttpMessageHandler
{
    private readonly bool _simulateInsufficientStock;
    public bool DeductStockCalled { get; private set; }

    public MockInventoryHandler(bool simulateInsufficientStock = false)
    {
        _simulateInsufficientStock = simulateInsufficientStock;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/deduct"))
        {
            DeductStockCalled = true;

            if (_simulateInsufficientStock)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = JsonContent.Create(new { error = "Insufficient stock" })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new InventoryItemDto
                {
                    Id = 1, ProductId = 1, ProductName = "Test", QuantityOnHand = 45,
                    ReorderLevel = 10, WarehouseLocation = "A-01", LastRestocked = DateTime.UtcNow
                })
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<InventoryItemDto>())
        });
    }
}
