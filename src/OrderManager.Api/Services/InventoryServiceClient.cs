using System.Net.Http.Json;

namespace OrderManager.Api.Services;

/// <summary>
/// HTTP client for communicating with the standalone inventory microservice.
/// Replaces direct database access for inventory operations.
/// </summary>
public class InventoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryServiceClient> _logger;

    public InventoryServiceClient(HttpClient httpClient, ILogger<InventoryServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<InventoryItemDto>> GetAllInventoryAsync()
    {
        _logger.LogInformation("Fetching all inventory from inventory-service");
        var items = await _httpClient.GetFromJsonAsync<List<InventoryItemDto>>("api/inventory");
        return items ?? new List<InventoryItemDto>();
    }

    public async Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        _logger.LogInformation("Fetching inventory for product {ProductId}", productId);
        var response = await _httpClient.GetAsync($"api/inventory/product/{productId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    public async Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        _logger.LogInformation("Restocking product {ProductId} with {Quantity}", productId, quantity);
        var response = await _httpClient.PostAsJsonAsync(
            $"api/inventory/product/{productId}/restock",
            new { Quantity = quantity });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>()
            ?? throw new InvalidOperationException("Restock returned null response");
    }

    public async Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        _logger.LogInformation("Fetching low-stock items from inventory-service");
        var items = await _httpClient.GetFromJsonAsync<List<InventoryItemDto>>("api/inventory/low-stock");
        return items ?? new List<InventoryItemDto>();
    }

    public async Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        _logger.LogInformation("Deducting {Quantity} from product {ProductId} via inventory-service", quantity, productId);
        var response = await _httpClient.PostAsJsonAsync(
            $"api/inventory/product/{productId}/deduct",
            new { Quantity = quantity });
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Insufficient stock for product {productId}. Service response: {error}");
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }
}

// InventoryItemDto is defined in InventoryService.cs
