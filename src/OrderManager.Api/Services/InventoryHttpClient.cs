using System.Net.Http.Json;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// HTTP client that calls the standalone inventory-service instead of accessing the database directly.
/// This replaces the in-process InventoryService as part of the monolith decomposition.
/// </summary>
public class InventoryHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryHttpClient> _logger;

    public InventoryHttpClient(HttpClient httpClient, ILogger<InventoryHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<InventoryItemDto>> GetAllInventoryAsync()
    {
        var response = await _httpClient.GetAsync("api/inventory");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryItemDto>>() ?? new List<InventoryItemDto>();
    }

    public async Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        var response = await _httpClient.GetAsync($"api/inventory/product/{productId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    public async Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/inventory/product/{productId}/restock", new { Quantity = quantity });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>()
            ?? throw new InvalidOperationException("Failed to deserialize restock response");
    }

    public async Task<InventoryItemDto> DeductStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/inventory/product/{productId}/deduct", new { Quantity = quantity });

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new InvalidOperationException(error?.Error ?? "Insufficient stock");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new ArgumentException($"No inventory record for product {productId}");

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>()
            ?? throw new InvalidOperationException("Failed to deserialize deduct response");
    }

    public async Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        var response = await _httpClient.GetAsync("api/inventory/low-stock");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryItemDto>>() ?? new List<InventoryItemDto>();
    }

    public async Task<bool> CheckStockAsync(int productId, int quantity)
    {
        var inventory = await GetInventoryByProductIdAsync(productId);
        return inventory is not null && inventory.QuantityOnHand >= quantity;
    }
}

/// <summary>
/// DTO for inventory items received from the inventory-service.
/// Separate from the monolith's InventoryItem entity which has EF navigation properties.
/// </summary>
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

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}
