using System.Net.Http.Json;

namespace OrderManager.Api.Clients;

/// <summary>
/// HTTP client implementation for communicating with the standalone inventory microservice.
/// Replaces direct database access for inventory operations.
/// </summary>
public class InventoryServiceHttpClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryServiceHttpClient> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="InventoryServiceHttpClient"/>.
    /// </summary>
    /// <param name="httpClient">Pre-configured <see cref="HttpClient"/> targeting the inventory-service base URL.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public InventoryServiceHttpClient(HttpClient httpClient, ILogger<InventoryServiceHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>Retrieves all inventory items from the inventory microservice.</summary>
    /// <returns>A list of all inventory items, or an empty list if the service returns no data.</returns>
    public async Task<List<InventoryItemDto>> GetAllInventoryAsync()
    {
        var response = await _httpClient.GetAsync("/api/inventory");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryItemDto>>() ?? new List<InventoryItemDto>();
    }

    /// <summary>Retrieves the inventory record for a specific product.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <returns>The inventory item, or <c>null</c> if no record exists.</returns>
    public async Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId)
    {
        var response = await _httpClient.GetAsync($"/api/inventory/product/{productId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    /// <summary>Restocks an inventory item by adding the specified quantity.</summary>
    /// <param name="productId">The product identifier to restock.</param>
    /// <param name="quantity">The number of units to add.</param>
    /// <returns>The updated inventory item after restocking.</returns>
    public async Task<InventoryItemDto> RestockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inventory/product/{productId}/restock", new { quantity });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>()
            ?? throw new InvalidOperationException("Failed to deserialize restock response");
    }

    /// <summary>Retrieves inventory items whose quantity is at or below the reorder threshold.</summary>
    /// <returns>A list of low-stock inventory items.</returns>
    public async Task<List<InventoryItemDto>> GetLowStockItemsAsync()
    {
        var response = await _httpClient.GetAsync("/api/inventory/low-stock");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryItemDto>>() ?? new List<InventoryItemDto>();
    }

    /// <summary>Checks whether the requested quantity is available for a product.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The quantity to check availability for.</param>
    /// <returns><c>true</c> if the requested quantity is available; otherwise <c>false</c>.</returns>
    public async Task<bool> CheckStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.GetAsync($"/api/inventory/product/{productId}/check?quantity={quantity}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StockCheckResult>();
        return result?.Available ?? false;
    }

    /// <summary>Deducts stock from a product's inventory via the microservice.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The number of units to deduct.</param>
    /// <returns>The updated inventory item, or <c>null</c> if insufficient stock (HTTP 409).</returns>
    public async Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inventory/product/{productId}/deduct", new { quantity });
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }
}
