using System.Net;
using System.Net.Http.Json;
using PactNet;
using Xunit;
using FluentAssertions;

namespace OrderService.Api.ContractTests;

public class InventoryApiPactTests
{
    private readonly IPactBuilderV4 _pactBuilder;

    public InventoryApiPactTests()
    {
        var pact = Pact.V4("OrderService", "InventoryService", new PactConfig
        {
            PactDir = Path.Combine("..", "..", "..", "pacts")
        });
        _pactBuilder = pact.WithHttpInteractions();
    }

    [Fact]
    public async Task ReserveStock_ReturnsSuccess_WhenSufficientStock()
    {
        _pactBuilder
            .UponReceiving("a request to reserve stock with sufficient quantity")
            .WithRequest(HttpMethod.Post, "/api/inventory/product/1/reserve")
            .WithJsonBody(new { quantity = 5 })
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new
            {
                success = true,
                message = "Stock reserved successfully"
            });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            var response = await client.PostAsJsonAsync("/api/inventory/product/1/reserve", new { quantity = 5 });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("true");
        });
    }

    [Fact]
    public async Task ReserveStock_ReturnsFailure_WhenInsufficientStock()
    {
        _pactBuilder
            .UponReceiving("a request to reserve stock with insufficient quantity")
            .WithRequest(HttpMethod.Post, "/api/inventory/product/1/reserve")
            .WithJsonBody(new { quantity = 99999 })
            .WillRespond()
            .WithStatus(HttpStatusCode.BadRequest)
            .WithJsonBody(new
            {
                success = false,
                message = "Insufficient stock"
            });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            var response = await client.PostAsJsonAsync("/api/inventory/product/1/reserve", new { quantity = 99999 });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        });
    }

    [Fact]
    public async Task ReleaseStock_ReturnsSuccess()
    {
        _pactBuilder
            .UponReceiving("a request to release reserved stock")
            .WithRequest(HttpMethod.Post, "/api/inventory/product/1/release")
            .WithJsonBody(new { quantity = 5 })
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new
            {
                success = true,
                message = "Stock released successfully"
            });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            var response = await client.PostAsJsonAsync("/api/inventory/product/1/release", new { quantity = 5 });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        });
    }
}
