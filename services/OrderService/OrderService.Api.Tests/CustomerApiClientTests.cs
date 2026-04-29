using System.Net;
using System.Net.Http.Json;
using Moq;
using Moq.Protected;
using OrderService.Api.Clients;
using FluentAssertions;
using Xunit;

namespace OrderService.Api.Tests;

public class CustomerApiClientTests
{
    private static CustomerApiClient CreateClient(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5010") };
        return new CustomerApiClient(httpClient);
    }

    [Fact]
    public async Task GetCustomerAsync_Returns_Customer_On200()
    {
        var customer = new CustomerDto { Id = 1, Name = "Test", Email = "t@t.com" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(customer)
        };
        var client = CreateClient(response);

        var result = await client.GetCustomerAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetCustomerAsync_ReturnsNull_On404()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var client = CreateClient(response);

        var result = await client.GetCustomerAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCustomerAsync_ThrowsOnServerError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var client = CreateClient(response);

        var act = () => client.GetCustomerAsync(1);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetCustomerAddressAsync_ReturnsAddress_On200()
    {
        var address = new CustomerAddressDto
        {
            Address = "123 Main St",
            City = "Springfield",
            State = "IL",
            ZipCode = "62701",
            FullAddress = "123 Main St, Springfield, IL 62701"
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(address)
        };
        var client = CreateClient(response);

        var result = await client.GetCustomerAddressAsync(1);

        result.Should().NotBeNull();
        result!.FullAddress.Should().Contain("123 Main St");
    }
}
