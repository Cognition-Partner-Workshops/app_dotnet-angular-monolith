using System.Net.Http.Json;
using System.Text.Json;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

public class InventoryServiceHttpClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InventoryServiceHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<InventoryItem>> GetAllInventoryAsync()
    {
        var response = await _httpClient.GetAsync("/api/inventory");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryItem>>(JsonOptions) ?? new List<InventoryItem>();
    }

    public async Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
    {
        var response = await _httpClient.GetAsync($"/api/inventory/product/{productId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItem>(JsonOptions);
    }

    public async Task<InventoryItem> RestockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inventory/product/{productId}/restock", new { quantity });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItem>(JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize restock response");
    }

    public async Task<InventoryItem> DeductStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inventory/product/{productId}/deduct", new { quantity });
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Insufficient stock for product {productId}: {error}");
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItem>(JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize deduct response");
    }

    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        var response = await _httpClient.GetAsync("/api/inventory/low-stock");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryItem>>(JsonOptions) ?? new List<InventoryItem>();
    }
}
