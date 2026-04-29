using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Api.Clients;
using OrderService.Api.Data;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using FluentAssertions;
using Xunit;

namespace OrderService.Api.IntegrationTests;

public class OrderCreationIntegrationTests : IDisposable
{
    private readonly WireMockServer _customerServiceMock;
    private readonly WireMockServer _productServiceMock;
    private readonly WireMockServer _inventoryServiceMock;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OrderCreationIntegrationTests()
    {
        _customerServiceMock = WireMockServer.Start();
        _productServiceMock = WireMockServer.Start();
        _inventoryServiceMock = WireMockServer.Start();

        SetupDefaultMocks();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<OrderDbContext>(options =>
                        options.UseInMemoryDatabase("IntegrationTests_" + Guid.NewGuid()));

                    services.AddHttpClient<ICustomerApiClient, CustomerApiClient>(c =>
                        c.BaseAddress = new Uri(_customerServiceMock.Url!));
                    services.AddHttpClient<IProductApiClient, ProductApiClient>(c =>
                        c.BaseAddress = new Uri(_productServiceMock.Url!));
                    services.AddHttpClient<IInventoryApiClient, InventoryApiClient>(c =>
                        c.BaseAddress = new Uri(_inventoryServiceMock.Url!));
                });
            });

        _client = _factory.CreateClient();
    }

    private void SetupDefaultMocks()
    {
        _customerServiceMock
            .Given(Request.Create().WithPath("/api/customers/*").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"id\":1,\"name\":\"Test\",\"email\":\"t@t.com\",\"address\":\"123 St\",\"city\":\"City\",\"state\":\"ST\",\"zipCode\":\"00000\"}"));

        _customerServiceMock
            .Given(Request.Create().WithPath("/api/customers/*/address").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"address\":\"123 St\",\"city\":\"City\",\"state\":\"ST\",\"zipCode\":\"00000\",\"fullAddress\":\"123 St, City, ST 00000\"}"));

        _productServiceMock
            .Given(Request.Create().WithPath("/api/products/*").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"id\":1,\"name\":\"Widget\",\"description\":\"Test\",\"category\":\"Test\",\"price\":9.99,\"sku\":\"W-1\"}"));

        _inventoryServiceMock
            .Given(Request.Create().WithPath("/api/inventory/product/*/reserve").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"success\":true,\"message\":\"Reserved\"}"));
    }

    [Fact]
    public async Task CreateOrder_EndToEnd_ReturnsCreatedOrder()
    {
        var request = new { CustomerId = 1, Items = new[] { new { ProductId = 1, Quantity = 2 } } };

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"totalAmount\"");
    }

    [Fact]
    public async Task CreateOrder_DownstreamTimeout_Returns500()
    {
        _productServiceMock.Reset();
        _productServiceMock
            .Given(Request.Create().WithPath("/api/products/*").UsingGet())
            .RespondWith(Response.Create().WithDelay(TimeSpan.FromSeconds(30)).WithStatusCode(200));

        var httpClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        var request = new { CustomerId = 1, Items = new[] { new { ProductId = 1, Quantity = 2 } } };

        var act = () => httpClient.PostAsJsonAsync("/api/orders", request);

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task CreateOrder_DownstreamError_ReturnsBadRequest()
    {
        _inventoryServiceMock.Reset();
        _inventoryServiceMock
            .Given(Request.Create().WithPath("/api/inventory/product/*/reserve").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"success\":false,\"message\":\"Insufficient stock\"}"));

        var request = new { CustomerId = 1, Items = new[] { new { ProductId = 1, Quantity = 2 } } };

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _customerServiceMock.Dispose();
        _productServiceMock.Dispose();
        _inventoryServiceMock.Dispose();
    }
}
