using System.Net.Http.Json;

namespace OrderManager.Api.Services;

public class InventoryServiceHttpClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;

    public InventoryServiceHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InventoryStockLevel?> GetStockLevelAsync(int productId)
    {
        var response = await _httpClient.GetAsync($"api/inventory/product/{productId}/stock-level");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<InventoryStockLevel>();
    }

    public async Task<bool> DeductStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/inventory/product/{productId}/deduct",
            new { Quantity = quantity });
        return response.IsSuccessStatusCode;
    }
}
