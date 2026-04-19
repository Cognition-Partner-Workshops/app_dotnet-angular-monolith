using System.Net.Http.Json;
using OrderManager.Api.HttpClients.Dtos;

namespace OrderManager.Api.HttpClients;

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
        _logger.LogInformation("Fetching all inventory from inventory-service");
        var response = await _httpClient.GetAsync("api/inventory");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryItemDto>>() ?? [];
    }

    public async Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        _logger.LogInformation("Fetching inventory for product {ProductId} from inventory-service", productId);
        var response = await _httpClient.GetAsync($"api/inventory/product/{productId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    public async Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        _logger.LogInformation("Restocking product {ProductId} with quantity {Quantity} via inventory-service", productId, quantity);
        var response = await _httpClient.PostAsJsonAsync($"api/inventory/product/{productId}/restock", new { Quantity = quantity });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>()
            ?? throw new InvalidOperationException("Failed to deserialize restock response");
    }

    public async Task<InventoryItemDto> DeductStockAsync(int productId, int quantity)
    {
        _logger.LogInformation("Deducting {Quantity} from product {ProductId} via inventory-service", quantity, productId);
        var response = await _httpClient.PostAsJsonAsync($"api/inventory/product/{productId}/deduct", new { Quantity = quantity });

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new InvalidOperationException(error?.Error ?? "Insufficient stock");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>()
            ?? throw new InvalidOperationException("Failed to deserialize deduct response");
    }

    public async Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        _logger.LogInformation("Fetching low stock items from inventory-service");
        var response = await _httpClient.GetAsync("api/inventory/low-stock");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryItemDto>>() ?? [];
    }
}

internal record ErrorResponse(string Error);
