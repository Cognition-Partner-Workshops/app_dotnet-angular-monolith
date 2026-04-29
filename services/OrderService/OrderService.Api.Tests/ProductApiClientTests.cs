using System.Net;
using System.Net.Http.Json;
using Moq;
using Moq.Protected;
using OrderService.Api.Clients;
using FluentAssertions;
using Xunit;

namespace OrderService.Api.Tests;

public class ProductApiClientTests
{
    private static ProductApiClient CreateClient(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:5020") };
        return new ProductApiClient(httpClient);
    }

    [Fact]
    public async Task GetProductAsync_ReturnsProduct_On200()
    {
        var product = new ProductDto { Id = 1, Name = "Widget", Price = 9.99m, Sku = "W-1" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(product)
        };
        var client = CreateClient(response);

        var result = await client.GetProductAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Widget");
        result.Price.Should().Be(9.99m);
    }

    [Fact]
    public async Task GetProductAsync_ReturnsNull_On404()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var client = CreateClient(response);

        var result = await client.GetProductAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProductAsync_ThrowsOnServerError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var client = CreateClient(response);

        var act = () => client.GetProductAsync(1);

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
