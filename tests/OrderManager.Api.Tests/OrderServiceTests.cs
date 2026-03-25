using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

public class MockInventoryServiceClient : IInventoryServiceClient
{
    private readonly AppDbContext _context;

    public MockInventoryServiceClient(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryItem>> GetAllInventoryAsync()
        => await _context.InventoryItems.ToListAsync();

    public async Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
        => await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);

    public async Task<InventoryItem> RestockAsync(int productId, int quantity)
    {
        var item = await _context.InventoryItems.FirstAsync(i => i.ProductId == productId);
        item.QuantityOnHand += quantity;
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
        => await _context.InventoryItems.Where(i => i.QuantityOnHand <= i.ReorderLevel).ToListAsync();

    public async Task<InventoryItem?> DeductStockAsync(int productId, int quantity)
    {
        var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
        if (item == null) return null;
        if (item.QuantityOnHand < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        item.QuantityOnHand -= quantity;
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<int> GetStockLevelAsync(int productId)
    {
        var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
        return item?.QuantityOnHand ?? 0;
    }
}

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

    private static InventoryHttpClient CreateMockInventoryClient(HttpStatusCode statusCode = HttpStatusCode.OK, InventoryItemDto? responseDto = null)
    {
        var dto = responseDto ?? new InventoryItemDto
        {
            Id = 1, ProductId = 1, ProductName = "Test", Sku = "TST-001",
            QuantityOnHand = 100, ReorderLevel = 10, WarehouseLocation = "A1",
            LastRestocked = DateTime.UtcNow
        };
        var json = JsonSerializer.Serialize(dto);
        var handler = new FakeHttpMessageHandler(statusCode, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5100") };
        return new InventoryHttpClient(httpClient);
    }

    private static InventoryHttpClient CreateMockInventoryClientWithError(HttpStatusCode statusCode, string errorMessage)
    {
        var json = JsonSerializer.Serialize(new { error = errorMessage });
        var handler = new FakeHttpMessageHandler(statusCode, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5100") };
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
    public async Task CreateOrder_CallsInventoryServiceToDeductStock()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient();
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

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
        var inventoryClient = CreateMockInventoryClientWithError(
            HttpStatusCode.Conflict, "Insufficient stock");
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }

    [Fact]
    public async Task CreateOrder_ThrowsWhenProductNotInInventory()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClientWithError(
            HttpStatusCode.NotFound, "No inventory record for product 999");
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 1) }));
    }
}

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseContent;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
    {
        _statusCode = statusCode;
        _responseContent = responseContent;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json")
        });
    }
}
