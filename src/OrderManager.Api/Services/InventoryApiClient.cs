using System.Net.Http.Json;

namespace OrderManager.Api.Services;

/// <summary>
/// DTO representing an inventory item returned by the inventory microservice.
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

/// <summary>
/// HTTP client for communicating with the inventory-service microservice.
/// Replaces the former in-process InventoryService that used AppDbContext directly.
/// </summary>
public class InventoryApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryApiClient> _logger;

    public InventoryApiClient(HttpClient httpClient, ILogger<InventoryApiClient> logger)
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
            ?? throw new InvalidOperationException("Restock returned null");
    }

    public async Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        var items = await _httpClient.GetFromJsonAsync<List<InventoryItemDto>>("api/inventory/low-stock");
        return items ?? new List<InventoryItemDto>();
    }

    public async Task<bool> CheckStockAsync(int productId, int quantity)
    {
        var result = await _httpClient.GetFromJsonAsync<StockCheckResult>($"api/inventory/product/{productId}/check?quantity={quantity}");
        return result?.Available ?? false;
    }

    public async Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/inventory/product/{productId}/deduct", new { Quantity = quantity });
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict ||
            response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    private record StockCheckResult(int ProductId, int Quantity, bool Available);
}
