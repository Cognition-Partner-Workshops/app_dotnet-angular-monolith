using System.Net.Http.Json;

namespace OrderManager.Api.Clients;

public class InventoryHttpClient : IInventoryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryHttpClient> _logger;

    public InventoryHttpClient(HttpClient httpClient, ILogger<InventoryHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<InventoryDto>> GetAllInventoryAsync()
    {
        _logger.LogInformation("Fetching all inventory from inventory-service");
        var response = await _httpClient.GetAsync("/api/inventory");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryDto>>() ?? new List<InventoryDto>();
    }

    public async Task<InventoryDto?> GetInventoryByProductIdAsync(int productId)
    {
        _logger.LogInformation("Fetching inventory for product {ProductId}", productId);
        var response = await _httpClient.GetAsync($"/api/inventory/product/{productId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryDto>();
    }

    public async Task<InventoryDto> RestockAsync(int productId, int quantity)
    {
        _logger.LogInformation("Restocking product {ProductId} with {Quantity}", productId, quantity);
        var response = await _httpClient.PostAsJsonAsync($"/api/inventory/product/{productId}/restock", new { quantity });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryDto>()
            ?? throw new InvalidOperationException("Failed to deserialize restock response");
    }

    public async Task<List<InventoryDto>> GetLowStockItemsAsync()
    {
        _logger.LogInformation("Fetching low-stock items from inventory-service");
        var response = await _httpClient.GetAsync("/api/inventory/low-stock");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryDto>>() ?? new List<InventoryDto>();
    }

    public async Task<bool> CheckStockAsync(int productId, int quantity)
    {
        _logger.LogInformation("Checking stock for product {ProductId}, quantity {Quantity}", productId, quantity);
        var response = await _httpClient.GetAsync($"/api/inventory/product/{productId}/check?quantity={quantity}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StockCheckResult>();
        return result?.Available ?? false;
    }

    public async Task<InventoryDto?> DeductStockAsync(int productId, int quantity)
    {
        _logger.LogInformation("Deducting {Quantity} from product {ProductId}", quantity, productId);
        var response = await _httpClient.PostAsJsonAsync($"/api/inventory/product/{productId}/deduct", new { quantity });
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Insufficient stock for product {productId}. Service response: {error}");
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryDto>();
    }
}

internal record StockCheckResult(int ProductId, int Quantity, bool Available);
