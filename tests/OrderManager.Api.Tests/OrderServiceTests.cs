using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
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

    private static InventoryApiClient CreateInventoryClient(bool deductSucceeds = true)
    {
        var handler = new FakeInventoryHandler(deductSucceeds);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://fake-inventory-service")
        };
        return new InventoryApiClient(httpClient);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var service = new OrderService(context, CreateInventoryClient());
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_CallsInventoryService()
    {
        using var context = CreateContext();
        var service = new OrderService(context, CreateInventoryClient(deductSucceeds: true));
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var service = new OrderService(context, CreateInventoryClient(deductSucceeds: false));
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

internal class FakeInventoryHandler : HttpMessageHandler
{
    private readonly bool _deductSucceeds;

    public FakeInventoryHandler(bool deductSucceeds)
    {
        _deductSucceeds = deductSucceeds;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/deduct"))
        {
            var status = _deductSucceeds ? HttpStatusCode.OK : HttpStatusCode.Conflict;
            var content = _deductSucceeds
                ? JsonContent.Create(new { message = "Stock deducted successfully" })
                : JsonContent.Create(new { message = "Insufficient stock" });
            return Task.FromResult(new HttpResponseMessage(status) { Content = content });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new object[] { })
        });
    }
}
