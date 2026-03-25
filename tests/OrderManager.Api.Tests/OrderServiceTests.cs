using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

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
        var mockClient = new Mock<IInventoryServiceClient>();
        var service = new OrderService(context, mockClient.Object);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockViaInventoryService()
    {
        using var context = CreateContext();
        var inventoryClient = CreateInventoryClient(deductSucceeds: true);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var mockClient = new Mock<IInventoryServiceClient>();
        mockClient.Setup(c => c.DeductStockAsync(product.Id, 5))
            .ReturnsAsync(new InventoryItem
            {
                Id = 1, ProductId = product.Id, ProductName = product.Name,
                QuantityOnHand = 45, ReorderLevel = 10, WarehouseLocation = "A-01"
            });

        var service = new OrderService(context, mockClient.Object);
        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Id, order.Items.First().ProductId);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = CreateInventoryClient(deductSucceeds: false);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var mockClient = new Mock<IInventoryServiceClient>();
        mockClient.Setup(c => c.DeductStockAsync(product.Id, 99999))
            .ThrowsAsync(new InvalidOperationException("Insufficient stock"));

        var service = new OrderService(context, mockClient.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenProductNotInInventory()
    {
        using var context = CreateContext();
        var inventoryClient = new FakeInventoryServiceClient(new Dictionary<int, int>());
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 1) }));
    }
}

/// <summary>
/// Fake HTTP handler for inventory service calls.
/// </summary>
public class FakeInventoryHandler : HttpMessageHandler
{
    private readonly bool _deductSucceeds;

    public FakeInventoryHandler(bool deductSucceeds = true)
    {
        _deductSucceeds = deductSucceeds;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/deduct"))
        {
            return Task.FromResult(new HttpResponseMessage(
                _deductSucceeds ? HttpStatusCode.OK : HttpStatusCode.Conflict));
        }

        // Default: return OK with empty array for list endpoints
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<InventoryItem>())
        };
        return Task.FromResult(response);
    }
}
