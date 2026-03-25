using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Services;
using Xunit;

namespace OrderManager.Api.Tests;

/// <summary>
/// Fake HTTP handler that simulates inventory-service responses for testing.
/// </summary>
public class FakeInventoryHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<int, int> _stock = new()
    {
        { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
    };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";

        if (path.Contains("/deduct"))
        {
            var segments = path.Split('/');
            var productIdStr = segments[^2];
            if (int.TryParse(productIdStr, out var pid) && _stock.TryGetValue(pid, out var currentQty))
            {
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(body);
                // Handle both PascalCase and camelCase property names
                int qty;
                if (doc.RootElement.TryGetProperty("Quantity", out var qPascal))
                    qty = qPascal.GetInt32();
                else if (doc.RootElement.TryGetProperty("quantity", out var qCamel))
                    qty = qCamel.GetInt32();
                else
                    throw new InvalidOperationException("No quantity property found in request body");

                if (currentQty < qty)
                {
                    return new HttpResponseMessage(HttpStatusCode.Conflict)
                    {
                        Content = new StringContent($"{{\"error\":\"Insufficient stock\"}}", System.Text.Encoding.UTF8, "application/json")
                    };
                }
                _stock[pid] = currentQty - qty;
                var item = new InventoryItemDto { Id = pid, ProductId = pid, QuantityOnHand = _stock[pid] };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(item), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
        };
    }

    public Task<List<InventoryItemDto>> GetAllInventoryAsync() =>
        Task.FromResult(_stock.Select(kv => new InventoryItemDto { ProductId = kv.Key, QuantityOnHand = kv.Value }).ToList());

    public Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_stock.ContainsKey(productId)
            ? new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] }
            : null);

    public Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
        return Task.FromResult(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public Task<InventoryItemDto> DeductStockAsync(int productId, int quantity)
    {
        if (!_stock.ContainsKey(productId))
            throw new InvalidOperationException($"No inventory record for product {productId}");
        if (_stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult(new InventoryItemDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public Task<List<InventoryItemDto>> GetLowStockItemsAsync() =>
        Task.FromResult(_stock.Where(kv => kv.Value <= 10).Select(kv => new InventoryItemDto { ProductId = kv.Key, QuantityOnHand = kv.Value }).ToList());
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

    private static InventoryServiceClient CreateMockInventoryClient(
        StockReservationResponse? reservationResponse = null)
    {
        var handler = new MockHttpMessageHandler(reservationResponse ?? new StockReservationResponse
        {
            Success = true,
            Message = "Stock reserved successfully"
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5002") };
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<InventoryServiceClient>();
        return new InventoryServiceClient(httpClient, logger);
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
    public async Task CreateOrder_ReservesStockViaInventoryService()
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
    public async Task CreateOrder_ThrowsOnFailedReservation()
    {
        using var context = CreateContext();
        var failedResponse = new StockReservationResponse
        {
            Success = false,
            Message = "Insufficient stock"
        };
        var inventoryClient = CreateMockInventoryClient(failedResponse);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly StockReservationResponse _reservationResponse;

    public MockHttpMessageHandler(StockReservationResponse reservationResponse)
    {
        _reservationResponse = reservationResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(_reservationResponse)
        };
        return Task.FromResult(response);
    }
}
