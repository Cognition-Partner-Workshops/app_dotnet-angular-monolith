using System.Net;
using System.Net.Http.Json;
using Moq;
using Moq.Protected;
using OrderService.Api.Clients;
using FluentAssertions;
using Xunit;

namespace OrderService.Api.Tests;

public class InventoryApiClientTests
{
    private static InventoryApiClient CreateClient(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5030") };
        return new InventoryApiClient(httpClient);
    }

    [Fact]
    public async Task ReserveStockAsync_ReturnsSuccess_On200()
    {
        var responseBody = new ReserveStockResponse { Success = true, Message = "Reserved" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(responseBody)
        };
        var client = CreateClient(response);

        var result = await client.ReserveStockAsync(1, 5);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ReserveStockAsync_ReturnsFailure_On400()
    {
        var responseBody = new ReserveStockResponse { Success = false, Message = "Insufficient stock" };
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(responseBody)
        };
        var client = CreateClient(response);

        var result = await client.ReserveStockAsync(1, 99999);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Insufficient");
    }

    [Fact]
    public async Task ReleaseStockAsync_ReturnsSuccess_On200()
    {
        var responseBody = new ReleaseStockResponse { Success = true, Message = "Released" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(responseBody)
        };
        var client = CreateClient(response);

        var result = await client.ReleaseStockAsync(1, 5);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ReserveStockAsync_ThrowsOnServerError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var client = CreateClient(response);

        var act = () => client.ReserveStockAsync(1, 5);

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
