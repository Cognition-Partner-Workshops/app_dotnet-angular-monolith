using System.Net.Http.Json;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

public class ProductServiceHttpClient : IProductService
{
    private readonly HttpClient _httpClient;

    public ProductServiceHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        var products = await _httpClient.GetFromJsonAsync<List<Product>>("api/products");
        return products ?? new List<Product>();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/products/{id}");
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<Product>();
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        var response = await _httpClient.PostAsJsonAsync("api/products", product);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<Product>();
        return created!;
    }

    public async Task<List<Product>> GetProductsByCategoryAsync(string category)
    {
        var products = await _httpClient.GetFromJsonAsync<List<Product>>($"api/products/category/{category}");
        return products ?? new List<Product>();
    }
}
