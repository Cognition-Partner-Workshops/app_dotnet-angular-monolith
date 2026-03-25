using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

/// <summary>
/// In-memory fake that overrides virtual methods to avoid real HTTP calls.
/// </summary>
public class FakeInventoryServiceClient : InventoryServiceClient
{
    private readonly Dictionary<int, int> _stock;
    private readonly bool _shouldFail;

    public FakeInventoryServiceClient(bool shouldFail = false)
        : base(new HttpClient(), NullLogger<InventoryServiceClient>.Instance)
    {
        _shouldFail = shouldFail;
        _stock = new Dictionary<int, int>
        {
            { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
        };
    }

    public override Task<List<InventoryItemDto>> GetAllInventoryAsync() =>
        Task.FromResult(_stock.Select(kv => new InventoryItemDto { ProductId = kv.Key, QuantityOnHand = kv.Value }).ToList());

    public override Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_stock.ContainsKey(productId)
            ? new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] }
            : null);

    public override Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
        return Task.FromResult(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public override Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        if (_shouldFail || !_stock.ContainsKey(productId) || _stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult<InventoryItemDto?>(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public override Task<List<InventoryItemDto>> GetLowStockItemsAsync() =>
        Task.FromResult(_stock.Where(kv => kv.Value <= 10)
            .Select(kv => new InventoryItemDto { ProductId = kv.Key, QuantityOnHand = kv.Value }).ToList());
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

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);
        var orders = await service.GetAllOrdersAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockViaMicroservice()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = new FakeInventoryServiceClient();
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var inventoryClient = new FakeInventoryServiceClient(shouldFail: true);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
