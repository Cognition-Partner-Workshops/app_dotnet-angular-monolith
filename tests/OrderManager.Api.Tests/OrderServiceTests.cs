using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

    private static InventoryServiceClient CreateMockInventoryClient(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5001") };
        return new InventoryServiceClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient(new HttpResponseMessage(HttpStatusCode.OK));
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();
        var inventory = await context.InventoryItems.FirstAsync(i => i.ProductId == product.Id);

        var inventoryDto = new InventoryItemDto
        {
            Id = inventory.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            QuantityOnHand = inventory.QuantityOnHand,
            ReorderLevel = inventory.ReorderLevel,
            WarehouseLocation = inventory.WarehouseLocation,
            LastRestocked = inventory.LastRestocked
        };

        var handler = new SequentialMockHandler(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(inventoryDto) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(inventoryDto) }
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5001") };
        var inventoryClient = new InventoryServiceClient(httpClient);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Id, order.Items.First().ProductId);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryDto = new InventoryItemDto
        {
            Id = 1,
            ProductId = product.Id,
            ProductName = product.Name,
            QuantityOnHand = 1,
            ReorderLevel = 10,
            WarehouseLocation = "A1",
            LastRestocked = DateTime.UtcNow
        };

        var inventoryClient = CreateMockInventoryClient(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(inventoryDto) });
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public MockHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }
}

public class SequentialMockHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage[] _responses;
    private int _callIndex;

    public SequentialMockHandler(HttpResponseMessage[] responses)
    {
        _responses = responses;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = _callIndex < _responses.Length ? _responses[_callIndex] : _responses[^1];
        _callIndex++;
        return Task.FromResult(response);
    }
}
