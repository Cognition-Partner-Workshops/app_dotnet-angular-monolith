using System.Net;
using System.Net.Http.Json;
using PactNet;
using Xunit;
using FluentAssertions;

namespace OrderService.Api.ContractTests;

public class CustomerApiPactTests
{
    private readonly IPactBuilderV4 _pactBuilder;

    public CustomerApiPactTests()
    {
        var pact = Pact.V4("OrderService", "CustomerService", new PactConfig
        {
            PactDir = Path.Combine("..", "..", "..", "pacts")
        });
        _pactBuilder = pact.WithHttpInteractions();
    }

    [Fact]
    public async Task GetCustomer_ReturnsCustomerWithAddressFields()
    {
        _pactBuilder
            .UponReceiving("a request to get a customer by id")
            .WithRequest(HttpMethod.Get, "/api/customers/1")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new
            {
                id = 1,
                name = "Acme Corp",
                email = "orders@acme.com",
                phone = "555-0100",
                address = "123 Main St",
                city = "Springfield",
                state = "IL",
                zipCode = "62701"
            });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            var response = await client.GetAsync("/api/customers/1");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Acme Corp");
        });
    }

    [Fact]
    public async Task GetCustomer_Returns404_WhenNotFound()
    {
        _pactBuilder
            .UponReceiving("a request to get a non-existent customer")
            .WithRequest(HttpMethod.Get, "/api/customers/9999")
            .WillRespond()
            .WithStatus(HttpStatusCode.NotFound);

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            var response = await client.GetAsync("/api/customers/9999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        });
    }

    [Fact]
    public async Task GetCustomerAddress_ReturnsAddressWithFullAddress()
    {
        _pactBuilder
            .UponReceiving("a request to get customer address")
            .WithRequest(HttpMethod.Get, "/api/customers/1/address")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new
            {
                address = "123 Main St",
                city = "Springfield",
                state = "IL",
                zipCode = "62701",
                fullAddress = "123 Main St, Springfield, IL 62701"
            });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            var response = await client.GetAsync("/api/customers/1/address");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("fullAddress");
        });
    }
}
