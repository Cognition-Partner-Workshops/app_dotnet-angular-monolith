using System.Net.Http.Json;

namespace OrderManager.Api.Services;

public class InventoryHttpClient
{
    private readonly HttpClient _httpClient;

    public InventoryHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InventoryDto?> GetInventoryByProductIdAsync(int productId)
    {
        var response = await _httpClient.GetAsync($"/api/inventory/product/{productId}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<InventoryDto>();
    }

    public async Task<List<InventoryDto>> GetAllInventoryAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<InventoryDto>>("/api/inventory") ?? new List<InventoryDto>();
    }

    public async Task<List<InventoryDto>> GetLowStockItemsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<InventoryDto>>("/api/inventory/low-stock") ?? new List<InventoryDto>();
    }

    public async Task<bool> CheckStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.GetFromJsonAsync<StockCheckResult>(
            $"/api/inventory/product/{productId}/check?quantity={quantity}");
        return response?.Available ?? false;
    }

    public async Task<InventoryDto?> DeductStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/inventory/product/{productId}/deduct", new { quantity });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<InventoryDto>();
    }

    public async Task<InventoryDto?> RestockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/inventory/product/{productId}/restock", new { quantity });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<InventoryDto>();
    }
}

public record InventoryDto(
    int Id,
    int ProductId,
    string ProductName,
    string Sku,
    int QuantityOnHand,
    int ReorderLevel,
    string WarehouseLocation,
    DateTime LastRestocked
);

public record StockCheckResult(int ProductId, int Quantity, bool Available);
