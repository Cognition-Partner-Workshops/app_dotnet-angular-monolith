using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ProductService.Api.Tests;

public class ProductContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProductContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ===== GET /api/products =====

    [Fact]
    public async Task GetAllProducts_ReturnsOkWithProductList()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(JsonOptions);
        Assert.NotNull(products);
        Assert.True(products.Count > 0, "Seeded database should return at least one product");
    }

    [Fact]
    public async Task GetAllProducts_ReturnsCorrectProductShape()
    {
        var response = await _client.GetAsync("/api/products");
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(JsonOptions);

        Assert.NotNull(products);
        var product = products.First();
        Assert.True(product.Id > 0);
        Assert.False(string.IsNullOrEmpty(product.Name));
        Assert.False(string.IsNullOrEmpty(product.Sku));
        Assert.False(string.IsNullOrEmpty(product.Category));
        Assert.True(product.Price > 0);
    }

    // ===== GET /api/products/{id} =====

    [Fact]
    public async Task GetProductById_ReturnsOkForExistingProduct()
    {
        var response = await _client.GetAsync("/api/products/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        Assert.NotNull(product);
        Assert.Equal(1, product.Id);
        Assert.False(string.IsNullOrEmpty(product.Name));
    }

    [Fact]
    public async Task GetProductById_ReturnsNotFoundForNonExistentProduct()
    {
        var response = await _client.GetAsync("/api/products/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ===== GET /api/products/category/{category} =====

    [Fact]
    public async Task GetProductsByCategory_ReturnsOkWithMatchingProducts()
    {
        var response = await _client.GetAsync("/api/products/category/Widgets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(JsonOptions);
        Assert.NotNull(products);
        Assert.True(products.Count > 0, "Should return products in the Widgets category");
        Assert.All(products, p => Assert.Equal("Widgets", p.Category));
    }

    [Fact]
    public async Task GetProductsByCategory_ReturnsEmptyListForNonExistentCategory()
    {
        var response = await _client.GetAsync("/api/products/category/NonExistentCategory");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(JsonOptions);
        Assert.NotNull(products);
        Assert.Empty(products);
    }

    // ===== POST /api/products =====

    [Fact]
    public async Task CreateProduct_ReturnsCreatedWithProduct()
    {
        var newProduct = new
        {
            Name = "Test Product",
            Description = "A test product",
            Category = "TestCategory",
            Price = 99.99m,
            Sku = $"TST-{Guid.NewGuid():N}"[..10]
        };

        var response = await _client.PostAsJsonAsync("/api/products", newProduct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal(newProduct.Name, created.Name);
        Assert.Equal(newProduct.Description, created.Description);
        Assert.Equal(newProduct.Category, created.Category);
        Assert.Equal(newProduct.Price, created.Price);
    }

    [Fact]
    public async Task CreateProduct_ReturnsLocationHeader()
    {
        var newProduct = new
        {
            Name = "Location Test Product",
            Description = "Testing location header",
            Category = "TestCategory",
            Price = 19.99m,
            Sku = $"LOC-{Guid.NewGuid():N}"[..10]
        };

        var response = await _client.PostAsJsonAsync("/api/products", newProduct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateProduct_CreatedProductIsRetrievable()
    {
        var newProduct = new
        {
            Name = "Retrievable Product",
            Description = "Should be retrievable after creation",
            Category = "TestCategory",
            Price = 49.99m,
            Sku = $"RET-{Guid.NewGuid():N}"[..10]
        };

        var createResponse = await _client.PostAsJsonAsync("/api/products", newProduct);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrieved = await getResponse.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(newProduct.Name, retrieved.Name);
    }

    // ===== Health endpoint =====

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ===== DTO for deserialization =====

    private class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Sku { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
