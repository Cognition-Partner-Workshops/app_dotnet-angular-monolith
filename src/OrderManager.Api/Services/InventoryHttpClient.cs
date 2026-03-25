using System.Net.Http.Json;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// HTTP client implementation for communicating with the standalone inventory microservice.
/// Replaces direct database access for inventory operations.
/// </summary>
public class InventoryHttpClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of <see cref="InventoryHttpClient"/>.
    /// </summary>
    /// <param name="httpClient">Pre-configured <see cref="HttpClient"/> targeting the inventory-service base URL.</param>
    public InventoryHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>Retrieves all inventory items from the inventory microservice.</summary>
    /// <returns>A list of all inventory items, or an empty list if the service returns no data.</returns>
    public async Task<List<InventoryItem>> GetAllInventoryAsync()
    {
        var response = await _httpClient.GetAsync("/api/inventory");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryItem>>() ?? new List<InventoryItem>();
    }

    /// <summary>Retrieves the inventory record for a specific product.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <returns>The inventory item, or <c>null</c> if no record exists for the given product.</returns>
    public async Task<InventoryItem?> GetInventoryByProductIdAsync(int productId)
    {
        var response = await _httpClient.GetAsync($"/api/inventory/product/{productId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItem>();
    }

    /// <summary>Restocks an inventory item by adding the specified quantity.</summary>
    /// <param name="productId">The product identifier to restock.</param>
    /// <param name="quantity">The number of units to add.</param>
    /// <returns>The updated inventory item after restocking.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service returns a null response.</exception>
    public async Task<InventoryItem> RestockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inventory/product/{productId}/restock", new { Quantity = quantity });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItem>()
            ?? throw new InvalidOperationException("Failed to deserialize restock response");
    }

    /// <summary>Deducts stock from a product's inventory via the microservice.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The number of units to deduct.</param>
    /// <returns>The updated inventory item after deduction.</returns>
    /// <exception cref="InvalidOperationException">Thrown when there is insufficient stock (HTTP 409).</exception>
    /// <exception cref="ArgumentException">Thrown when the product has no inventory record (HTTP 404).</exception>
    public async Task<InventoryItem> DeductStockAsync(int productId, int quantity)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inventory/product/{productId}/deduct", new { Quantity = quantity });

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new InvalidOperationException(error?.Error ?? "Insufficient stock");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new ArgumentException(error?.Error ?? $"No inventory record for product {productId}");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItem>()
            ?? throw new InvalidOperationException("Failed to deserialize deduct response");
    }

    /// <summary>Retrieves inventory items whose quantity is at or below the reorder threshold.</summary>
    /// <returns>A list of low-stock inventory items.</returns>
    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        var response = await _httpClient.GetAsync("/api/inventory/low-stock");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InventoryItem>>() ?? new List<InventoryItem>();
    }
}

/// <summary>Standard error response envelope from the inventory microservice.</summary>
public class ErrorResponse
{
    /// <summary>Human-readable error message.</summary>
    public string Error { get; set; } = string.Empty;
}
