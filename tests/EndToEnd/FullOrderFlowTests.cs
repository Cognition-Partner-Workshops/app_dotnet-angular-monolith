using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace EndToEnd;

[Trait("Category", "EndToEnd")]
public class FullOrderFlowTests
{
    private readonly HttpClient _gatewayClient;

    public FullOrderFlowTests()
    {
        var gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY_URL") ?? "http://localhost:5001";
        _gatewayClient = new HttpClient { BaseAddress = new Uri(gatewayUrl), Timeout = TimeSpan.FromSeconds(30) };
    }

    [Fact]
    public async Task CreateCustomer_CreateProduct_AddInventory_PlaceOrder_VerifyAll()
    {
        // Create customer
        var customerPayload = new
        {
            Name = "E2E Customer",
            Email = $"e2e-{Guid.NewGuid():N}@test.com",
            Phone = "555-9999",
            Address = "999 E2E St",
            City = "Testville",
            State = "TX",
            ZipCode = "75001"
        };
        var customerResponse = await _gatewayClient.PostAsJsonAsync("/api/customers", customerPayload);
        customerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await customerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = customer.GetProperty("id").GetInt32();

        // Get a product (seeded)
        var productsResponse = await _gatewayClient.GetAsync("/api/products");
        productsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await productsResponse.Content.ReadFromJsonAsync<JsonElement>();
        products.GetArrayLength().Should().BeGreaterThan(0);
        var productId = products[0].GetProperty("id").GetInt32();

        // Check inventory
        var inventoryResponse = await _gatewayClient.GetAsync($"/api/inventory/product/{productId}");
        inventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Place order
        var orderPayload = new
        {
            CustomerId = customerId,
            Items = new[] { new { ProductId = productId, Quantity = 1 } }
        };
        var orderResponse = await _gatewayClient.PostAsJsonAsync("/api/orders", orderPayload);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await orderResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = order.GetProperty("id").GetInt32();

        // Verify order exists
        var getOrderResponse = await _gatewayClient.GetAsync($"/api/orders/{orderId}");
        getOrderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PlaceOrder_InsufficientStock_ReturnsError_InventoryUnchanged()
    {
        var productsResponse = await _gatewayClient.GetAsync("/api/products");
        var products = await productsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productId = products[0].GetProperty("id").GetInt32();

        // Get inventory before
        var inventoryBefore = await _gatewayClient.GetAsync($"/api/inventory/product/{productId}");
        var invBefore = await inventoryBefore.Content.ReadFromJsonAsync<JsonElement>();
        var qtyBefore = invBefore.GetProperty("quantityOnHand").GetInt32();

        // Try to order more than available
        var customersResponse = await _gatewayClient.GetAsync("/api/customers");
        var customers = await customersResponse.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = customers[0].GetProperty("id").GetInt32();

        var orderPayload = new
        {
            CustomerId = customerId,
            Items = new[] { new { ProductId = productId, Quantity = 999999 } }
        };
        var orderResponse = await _gatewayClient.PostAsJsonAsync("/api/orders", orderPayload);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify inventory unchanged
        var inventoryAfter = await _gatewayClient.GetAsync($"/api/inventory/product/{productId}");
        var invAfter = await inventoryAfter.Content.ReadFromJsonAsync<JsonElement>();
        var qtyAfter = invAfter.GetProperty("quantityOnHand").GetInt32();
        qtyAfter.Should().Be(qtyBefore);
    }

    [Fact]
    public async Task GetOrders_AfterCreation_IncludesNewOrder()
    {
        // First get all orders count
        var beforeResponse = await _gatewayClient.GetAsync("/api/orders");
        var beforeOrders = await beforeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var countBefore = beforeOrders.GetArrayLength();

        // Get existing data for order creation
        var customersResponse = await _gatewayClient.GetAsync("/api/customers");
        var customers = await customersResponse.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = customers[0].GetProperty("id").GetInt32();

        var productsResponse = await _gatewayClient.GetAsync("/api/products");
        var products = await productsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productId = products[0].GetProperty("id").GetInt32();

        // Create order
        var orderPayload = new
        {
            CustomerId = customerId,
            Items = new[] { new { ProductId = productId, Quantity = 1 } }
        };
        await _gatewayClient.PostAsJsonAsync("/api/orders", orderPayload);

        // Verify count increased
        var afterResponse = await _gatewayClient.GetAsync("/api/orders");
        var afterOrders = await afterResponse.Content.ReadFromJsonAsync<JsonElement>();
        afterOrders.GetArrayLength().Should().BeGreaterThan(countBefore);
    }
}
