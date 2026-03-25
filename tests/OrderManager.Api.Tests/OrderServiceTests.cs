using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

public class FakeInventoryServiceClient : IInventoryServiceClient
{
    private readonly Dictionary<int, int> _stock;
    private readonly bool _shouldFail;

    public FakeInventoryServiceClient(bool shouldFail = false)
    {
        _shouldFail = shouldFail;
        _stock = new Dictionary<int, int>
        {
            { 1, 50 }, { 2, 100 }, { 3, 150 }, { 4, 200 }, { 5, 250 }
        };
    }

    public Task<List<InventoryDto>> GetAllInventoryAsync() =>
        Task.FromResult(_stock.Select(kv => new InventoryDto { ProductId = kv.Key, QuantityOnHand = kv.Value }).ToList());

    public Task<InventoryDto?> GetInventoryByProductIdAsync(int productId) =>
        Task.FromResult(_stock.ContainsKey(productId)
            ? new InventoryDto { ProductId = productId, QuantityOnHand = _stock[productId] }
            : null);

    public Task<InventoryDto> RestockAsync(int productId, int quantity)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
        return Task.FromResult(new InventoryDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public Task<InventoryDto> DeductStockAsync(int productId, int quantity)
    {
        if (_shouldFail || !_stock.ContainsKey(productId) || _stock[productId] < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {productId}");
        _stock[productId] -= quantity;
        return Task.FromResult(new InventoryDto { ProductId = productId, QuantityOnHand = _stock[productId] });
    }

    public Task<List<InventoryDto>> GetLowStockItemsAsync() =>
        Task.FromResult(_stock.Where(kv => kv.Value <= 10)
            .Select(kv => new InventoryDto { ProductId = kv.Key, QuantityOnHand = kv.Value }).ToList());
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

    private static InventoryServiceClient CreateInventoryServiceClient(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string? jsonContent = null)
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var response = new HttpResponseMessage(statusCode);
                if (jsonContent != null)
                    response.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                else if (statusCode == HttpStatusCode.OK)
                    response.Content = new StringContent(
                        "{\"success\":true,\"message\":\"Stock reserved\",\"details\":[]}",
                        System.Text.Encoding.UTF8,
                        "application/json");
                return response;
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:5100") };
        var logger = new Mock<ILogger<InventoryServiceClient>>();
        return new InventoryServiceClient(httpClient, logger.Object);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyList_WhenNoOrders()
    {
        using var context = CreateContext();
        var inventoryClient = CreateInventoryServiceClient();
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

        var inventoryClient = CreateInventoryServiceClient(HttpStatusCode.OK);
        var service = new OrderService(context, inventoryClient);

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Price * 5, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var failJson = "{\"success\":false,\"message\":\"Insufficient stock\",\"details\":[]}";
        var inventoryClient = CreateInventoryServiceClient(HttpStatusCode.OK, failJson);
        var service = new OrderService(context, inventoryClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}
