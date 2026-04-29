using System.Net;
using PactNet;
using Xunit;
using FluentAssertions;

namespace OrderService.Api.ContractTests;

public class ProductApiPactTests
{
    private readonly IPactBuilderV4 _pactBuilder;

    public ProductApiPactTests()
    {
        var pact = Pact.V4("OrderService", "ProductService", new PactConfig
        {
            PactDir = Path.Combine("..", "..", "..", "pacts")
        });
        _pactBuilder = pact.WithHttpInteractions();
    }

    [Fact]
    public async Task GetProduct_ReturnsProductWithPrice()
    {
        _pactBuilder
            .UponReceiving("a request to get a product by id")
            .WithRequest(HttpMethod.Get, "/api/products/1")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new
            {
                id = 1,
                name = "Widget A",
                description = "Standard widget",
                category = "Widgets",
                price = 9.99,
                sku = "WGT-001"
            });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            var response = await client.GetAsync("/api/products/1");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Widget A");
            content.Should().Contain("price");
        });
    }

    [Fact]
    public async Task GetProduct_Returns404_WhenNotFound()
    {
        _pactBuilder
            .UponReceiving("a request to get a non-existent product")
            .WithRequest(HttpMethod.Get, "/api/products/9999")
            .WillRespond()
            .WithStatus(HttpStatusCode.NotFound);

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            var response = await client.GetAsync("/api/products/9999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        });
    }
}
