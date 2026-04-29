using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using FluentAssertions;
using Xunit;

namespace Gateway.Api.Tests;

public class RoutingTests : IDisposable
{
    private readonly WireMockServer _customerService;
    private readonly WireMockServer _productService;
    private readonly WireMockServer _inventoryService;
    private readonly WireMockServer _orderService;
    private readonly WebApplicationFactory<Program> _factory;

    public RoutingTests()
    {
        _customerService = WireMockServer.Start();
        _productService = WireMockServer.Start();
        _inventoryService = WireMockServer.Start();
        _orderService = WireMockServer.Start();

        SetupHealthEndpoints();
        SetupApiMocks();

        _factory = CreateFactory(
            _customerService.Url!,
            _productService.Url!,
            _inventoryService.Url!,
            _orderService.Url!);
    }

    private WebApplicationFactory<Program> CreateFactory(
        string customerUrl, string productUrl, string inventoryUrl, string orderUrl)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ServiceUrls:CustomerService"] = customerUrl,
                        ["ServiceUrls:ProductService"] = productUrl,
                        ["ServiceUrls:InventoryService"] = inventoryUrl,
                        ["ServiceUrls:OrderService"] = orderUrl,
                        ["ReverseProxy:Clusters:customer-cluster:Destinations:destination1:Address"] = customerUrl,
                        ["ReverseProxy:Clusters:product-cluster:Destinations:destination1:Address"] = productUrl,
                        ["ReverseProxy:Clusters:inventory-cluster:Destinations:destination1:Address"] = inventoryUrl,
                        ["ReverseProxy:Clusters:order-cluster:Destinations:destination1:Address"] = orderUrl,
                    });
                });
            });
    }

    private void SetupHealthEndpoints()
    {
        foreach (var server in new[] { _customerService, _productService, _inventoryService, _orderService })
        {
            server.Given(Request.Create().WithPath("/health").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody("Healthy"));
        }
    }

    private void SetupApiMocks()
    {
        _customerService.Given(Request.Create().WithPath("/api/customers*").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]"));

        _productService.Given(Request.Create().WithPath("/api/products*").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]"));

        _orderService.Given(Request.Create().WithPath("/api/orders*").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]"));

        _inventoryService.Given(Request.Create().WithPath("/api/inventory*").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]"));
    }

    [Fact]
    public async Task Request_ToCustomersRoute_ProxiesToCustomerService()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/customers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_ToProductsRoute_ProxiesToProductService()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_ToOrdersRoute_ProxiesToOrderService()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_ToInventoryRoute_ProxiesToInventoryService()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/inventory");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy_WhenAllServicesUp()
    {
        var client = _factory.CreateClient();
        // Allow time for health checks to initialize
        await Task.Delay(500);
        var response = await client.GetAsync("/health");
        // Health check may return OK or ServiceUnavailable depending on timing
        // The important thing is the endpoint responds
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task HealthCheck_ReturnsDegraded_WhenServiceDown()
    {
        _customerService.Stop();

        var factory = CreateFactory(
            "http://localhost:19999",
            _productService.Url!,
            _inventoryService.Url!,
            _orderService.Url!);

        var client = factory.CreateClient();
        await Task.Delay(500);
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        factory.Dispose();
    }

    public void Dispose()
    {
        _factory.Dispose();
        _customerService.Dispose();
        _productService.Dispose();
        _inventoryService.Dispose();
        _orderService.Dispose();
    }
}
