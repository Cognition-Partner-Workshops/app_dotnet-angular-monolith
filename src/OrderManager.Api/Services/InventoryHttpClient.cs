using System.Net.Http.Json;

namespace OrderManager.Api.Services;

public class InventoryHttpClient
{
    private readonly HttpClient _httpClient;

    public InventoryHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<InventoryItemDto>> GetAllInventoryAsync()
    {
        var items = await _httpClient.GetFromJsonAsync<List<InventoryItemDto>>("api/inventory");
        return items ?? new List<InventoryItemDto>();
    }

    public async Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        var response = await _httpClient.GetAsync($"api/inventory/product/{productId}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    public async Task<InventoryItemDto?> RestockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/inventory/product/{productId}/restock",
            new { quantity });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    public async Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        var items = await _httpClient.GetFromJsonAsync<List<InventoryItemDto>>("api/inventory/low-stock");
        return items ?? new List<InventoryItemDto>();
    }

    public async Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/inventory/product/{productId}/deduct",
            new { quantity });

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new InvalidOperationException(error?.Error ?? "Insufficient stock");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    public async Task<int> GetStockLevelAsync(int productId)
    {
        var response = await _httpClient.GetFromJsonAsync<StockLevelResponse>(
            $"api/inventory/product/{productId}/stock-level");
        return response?.QuantityOnHand ?? 0;
    }
}

public class InventoryItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; }
}

public class StockLevelResponse
{
    public int ProductId { get; set; }
    public int QuantityOnHand { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}
