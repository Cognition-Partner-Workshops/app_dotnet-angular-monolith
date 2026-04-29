using System.Net;
using System.Net.Http.Json;
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
        var customer = await customerResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var customerId = Convert.ToInt32(customer!["id"]);

        // Get a product (seeded)
        var productsResponse = await _gatewayClient.GetAsync("/api/products");
        productsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await productsResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        products.Should().NotBeEmpty();
        var productId = Convert.ToInt32(products![0]["id"]);

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
        var order = await orderResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var orderId = Convert.ToInt32(order!["id"]);

        // Verify order exists
        var getOrderResponse = await _gatewayClient.GetAsync($"/api/orders/{orderId}");
        getOrderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PlaceOrder_InsufficientStock_ReturnsError_InventoryUnchanged()
    {
        var productsResponse = await _gatewayClient.GetAsync("/api/products");
        var products = await productsResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        var productId = Convert.ToInt32(products![0]["id"]);

        // Get inventory before
        var inventoryBefore = await _gatewayClient.GetAsync($"/api/inventory/product/{productId}");
        var invBefore = await inventoryBefore.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var qtyBefore = Convert.ToInt32(invBefore!["quantityOnHand"]);

        // Try to order more than available
        var customersResponse = await _gatewayClient.GetAsync("/api/customers");
        var customers = await customersResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        var customerId = Convert.ToInt32(customers![0]["id"]);

        var orderPayload = new
        {
            CustomerId = customerId,
            Items = new[] { new { ProductId = productId, Quantity = 999999 } }
        };
        var orderResponse = await _gatewayClient.PostAsJsonAsync("/api/orders", orderPayload);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify inventory unchanged
        var inventoryAfter = await _gatewayClient.GetAsync($"/api/inventory/product/{productId}");
        var invAfter = await inventoryAfter.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var qtyAfter = Convert.ToInt32(invAfter!["quantityOnHand"]);
        qtyAfter.Should().Be(qtyBefore);
    }

    [Fact]
    public async Task GetOrders_AfterCreation_IncludesNewOrder()
    {
        // First get all orders count
        var beforeResponse = await _gatewayClient.GetAsync("/api/orders");
        var beforeOrders = await beforeResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        var countBefore = beforeOrders!.Count;

        // Get existing data for order creation
        var customersResponse = await _gatewayClient.GetAsync("/api/customers");
        var customers = await customersResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        var customerId = Convert.ToInt32(customers![0]["id"]);

        var productsResponse = await _gatewayClient.GetAsync("/api/products");
        var products = await productsResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        var productId = Convert.ToInt32(products![0]["id"]);

        // Create order
        var orderPayload = new
        {
            CustomerId = customerId,
            Items = new[] { new { ProductId = productId, Quantity = 1 } }
        };
        await _gatewayClient.PostAsJsonAsync("/api/orders", orderPayload);

        // Verify count increased
        var afterResponse = await _gatewayClient.GetAsync("/api/orders");
        var afterOrders = await afterResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        afterOrders!.Count.Should().BeGreaterThan(countBefore);
    }
}
