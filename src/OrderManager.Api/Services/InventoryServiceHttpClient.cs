using System.Net.Http.Json;
using System.Text.Json;

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

    public async Task<List<InventoryDto>> GetAllInventoryAsync()
    {
        var response = await _httpClient.GetAsync("/api/inventory");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryDto>>(JsonOptions)
            ?? new List<InventoryDto>();
    }

    public async Task<InventoryDto?> GetInventoryByProductIdAsync(int productId)
    {
        var response = await _httpClient.GetAsync($"/api/inventory/product/{productId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions);
    }

    public async Task<InventoryDto> RestockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/inventory/product/{productId}/restock",
            new { quantity }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize restock response");
    }

    public async Task<InventoryDto> DeductStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/inventory/product/{productId}/deduct",
            new { quantity }, JsonOptions);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(
                $"Insufficient stock for product {productId}");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new ArgumentException(
                $"No inventory record for product {productId}");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryDto>(JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize deduct response");
    }

    public async Task<List<InventoryDto>> GetLowStockItemsAsync()
    {
        var response = await _httpClient.GetAsync("/api/inventory/low-stock");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryDto>>(JsonOptions)
            ?? new List<InventoryDto>();
    }
}
