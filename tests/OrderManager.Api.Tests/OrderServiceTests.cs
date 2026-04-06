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

    private static InventoryServiceClient CreateMockInventoryClient(
        bool stockAvailable = true, bool deductSucceeds = true)
    {
        var handler = new FakeInventoryHandler(stockAvailable, deductSucceeds);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5001") };
        var logger = LoggerFactory.Create(b => { }).CreateLogger<InventoryServiceClient>();
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
    public async Task CreateOrder_DeductsInventoryViaHttpClient()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient(stockAvailable: true, deductSucceeds: true);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var order = await service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 5) });

        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(product.Id, order.Items.First().ProductId);
    }

    [Fact]
    public async Task CreateOrder_ThrowsOnInsufficientStock()
    {
        using var context = CreateContext();
        var inventoryClient = CreateMockInventoryClient(stockAvailable: false);
        var service = new OrderService(context, inventoryClient);
        var product = await context.Products.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(customer.Id, new List<(int, int)> { (product.Id, 99999) }));
    }
}

/// <summary>
/// Fake HTTP handler that simulates the inventory-service API responses for testing.
/// </summary>
internal class FakeInventoryHandler : HttpMessageHandler
{
    private readonly bool _stockAvailable;
    private readonly bool _deductSucceeds;

    public FakeInventoryHandler(bool stockAvailable, bool deductSucceeds)
    {
        _stockAvailable = stockAvailable;
        _deductSucceeds = deductSucceeds;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";

        // GET /api/inventory/product/{id} — used by CheckStockAsync
        if (request.Method == HttpMethod.Get && path.StartsWith("/api/inventory/product/"))
        {
            var dto = new InventoryItemDto
            {
                Id = 1,
                ProductId = 1,
                ProductName = "Test Product",
                Sku = "TST-001",
                QuantityOnHand = _stockAvailable ? 1000 : 0,
                ReorderLevel = 10,
                WarehouseLocation = "A-01",
                LastRestocked = DateTime.UtcNow
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dto)
            });
        }

        // POST /api/inventory/product/{id}/deduct
        if (request.Method == HttpMethod.Post && path.Contains("/deduct"))
        {
            if (!_deductSucceeds)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Insufficient stock")
                });
            }
            var dto = new InventoryItemDto
            {
                Id = 1, ProductId = 1, ProductName = "Test Product", Sku = "TST-001",
                QuantityOnHand = 995, ReorderLevel = 10, WarehouseLocation = "A-01",
                LastRestocked = DateTime.UtcNow
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dto)
            });
        }

        // Default: OK with empty array
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<InventoryItemDto>())
        });
    }
}
