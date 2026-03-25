using System.Net.Http.Json;

namespace OrderManager.Api.Services;

/// <summary>
/// HTTP client that calls the standalone inventory microservice.
/// Replaces the former in-process InventoryService that used EF Core directly.
/// </summary>
public class InventoryServiceClient
{
    private readonly HttpClient _httpClient;

    public InventoryServiceClient(HttpClient httpClient)
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
        var items = await _httpClient.GetFromJsonAsync<List<InventoryItemDto>>("api/inventory/low-stock");
        return items ?? new List<InventoryItemDto>();
    }

    public async Task<StockCheckResponse> CheckStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.GetFromJsonAsync<StockCheckResponse>(
            $"api/inventory/product/{productId}/check?quantity={quantity}");
        return response ?? new StockCheckResponse(productId, 0, false);
    }

    public async Task DeductStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/inventory/product/{productId}/deduct",
            new { Quantity = quantity });
        response.EnsureSuccessStatusCode();
    }
}

/// <summary>
/// DTO matching the inventory microservice response shape.
/// </summary>
public record InventoryItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string Sku,
    int QuantityOnHand,
    int ReorderLevel,
    string WarehouseLocation,
    DateTime LastRestocked);

/// <summary>
/// DTO for stock check responses from the inventory microservice.
/// </summary>
public record StockCheckResponse(
    int ProductId,
    int QuantityOnHand,
    bool InStock);
