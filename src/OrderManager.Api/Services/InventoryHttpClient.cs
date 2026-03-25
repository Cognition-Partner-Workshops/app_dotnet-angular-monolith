using System.Net.Http.Json;

namespace OrderManager.Api.Services;

/// <summary>
/// HTTP client that proxies inventory operations to the standalone inventory-service microservice.
/// Replaces the former in-process InventoryService that accessed the shared database directly.
/// </summary>
public class InventoryHttpClient
{
    private readonly HttpClient _httpClient;

    public InventoryHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<InventoryItem>> GetAllInventoryAsync()
    {
        var items = await _httpClient.GetFromJsonAsync<List<InventoryItemDto>>("api/inventory");
        return items?.Select(MapToInventoryItem).ToList() ?? new List<InventoryItem>();
    }

    public async Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
    {
        var response = await _httpClient.GetAsync($"api/inventory/product/{productId}");
        if (!response.IsSuccessStatusCode) return null;
        var dto = await response.Content.ReadFromJsonAsync<InventoryItemDto>();
        return dto is null ? null : MapToInventoryItem(dto);
    }

    public async Task<InventoryItem> RestockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/inventory/product/{productId}/restock", new { quantity });
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<InventoryItemDto>()
            ?? throw new InvalidOperationException("Failed to deserialize restock response");
        return MapToInventoryItem(dto);
    }

    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        var items = await _httpClient.GetFromJsonAsync<List<InventoryItemDto>>("api/inventory/low-stock");
        return items?.Select(MapToInventoryItem).ToList() ?? new List<InventoryItem>();
    }

    public async Task<bool> DeductStockAsync(int productId, int quantity)
    {
        var result = await _httpClient.GetFromJsonAsync<StockCheckResult>($"api/inventory/product/{productId}/check?quantity={quantity}");
        return result?.Available ?? false;
    }

    public async Task<InventoryItem> DeductStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/inventory/product/{productId}/deduct", new { quantity });
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to deduct stock for product {productId}: {error}");
        }
        var dto = await response.Content.ReadFromJsonAsync<InventoryItemDto>()
            ?? throw new InvalidOperationException("Failed to deserialize deduct response");
        return MapToInventoryItem(dto);
    }

    private static InventoryItem MapToInventoryItem(InventoryItemDto dto)
    {
        return new InventoryItem
        {
            Id = dto.Id,
            ProductId = dto.ProductId,
            QuantityOnHand = dto.QuantityOnHand,
            ReorderLevel = dto.ReorderLevel,
            WarehouseLocation = dto.WarehouseLocation,
            LastRestocked = dto.LastRestocked
        };
    }
}

public class InventoryItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; }
}

public class StockLevelDto
{
    public int ProductId { get; set; }
    public int QuantityOnHand { get; set; }
}
