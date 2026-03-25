using System.Net.Http.Json;

namespace OrderManager.Api.Clients;

public class InventoryHttpClient : IInventoryClient
{
    private readonly HttpClient _httpClient;

    public InventoryHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<InventoryItemDto>> GetAllInventoryAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<InventoryItemDto>>("api/inventory")
            ?? new List<InventoryItemDto>();
    }

    public async Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        var response = await _httpClient.GetAsync($"api/inventory/product/{productId}");
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    public async Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/inventory/product/{productId}/restock",
            new { Quantity = quantity });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InventoryItemDto>())!;
    }

    public async Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<InventoryItemDto>>("api/inventory/low-stock")
            ?? new List<InventoryItemDto>();
    }

    public async Task<bool> CheckStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.GetAsync(
            $"api/inventory/product/{productId}/check?quantity={quantity}");
        if (!response.IsSuccessStatusCode)
            return false;
        var result = await response.Content.ReadFromJsonAsync<StockCheckResult>();
        return result?.Available ?? false;
    }

    public async Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/inventory/product/{productId}/deduct",
            new { Quantity = quantity });
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    private record StockCheckResult(int ProductId, int Quantity, bool Available);
}
