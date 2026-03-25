using System.Net.Http.Json;

namespace OrderManager.Api.Services;

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

public interface IInventoryServiceClient
{
    Task<List<InventoryItemDto>> GetAllInventoryAsync();
    Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId);
    Task<InventoryItemDto> RestockAsync(int productId, int quantity);
    Task<List<InventoryItemDto>> GetLowStockItemsAsync();
    Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity);
    Task<int> GetStockLevelAsync(int productId);
}

public class InventoryServiceHttpClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryServiceHttpClient> _logger;

    public InventoryServiceHttpClient(HttpClient httpClient, ILogger<InventoryServiceHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<InventoryItemDto>> GetAllInventoryAsync()
    {
        _logger.LogInformation("Fetching all inventory from inventory-service");
        var response = await _httpClient.GetAsync("/api/inventory");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryItemDto>>() ?? new List<InventoryItemDto>();
    }

    public async Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        _logger.LogInformation("Fetching inventory for product {ProductId} from inventory-service", productId);
        var response = await _httpClient.GetAsync($"/api/inventory/product/{productId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    public async Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        _logger.LogInformation("Restocking product {ProductId} with {Quantity} via inventory-service", productId, quantity);
        var response = await _httpClient.PostAsJsonAsync($"/api/inventory/product/{productId}/restock", new { quantity });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>()
            ?? throw new InvalidOperationException("Failed to deserialize restock response");
    }

    public async Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        _logger.LogInformation("Fetching low-stock items from inventory-service");
        var response = await _httpClient.GetAsync("/api/inventory/low-stock");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryItemDto>>() ?? new List<InventoryItemDto>();
    }

    public async Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        _logger.LogInformation("Deducting {Quantity} from product {ProductId} via inventory-service", quantity, productId);
        var response = await _httpClient.PostAsJsonAsync($"/api/inventory/product/{productId}/deduct", new { quantity });
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Insufficient stock for product {productId}. Service response: {error}");
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    public async Task<int> GetStockLevelAsync(int productId)
    {
        _logger.LogInformation("Fetching stock level for product {ProductId} from inventory-service", productId);
        var response = await _httpClient.GetAsync($"/api/inventory/product/{productId}/stock-level");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StockLevelResponse>();
        return result?.QuantityOnHand ?? 0;
    }
}

internal record StockLevelResponse(int ProductId, int QuantityOnHand);
