using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Interface for inventory service client, enabling testability.
/// The concrete implementation (InventoryHttpClient) makes HTTP calls
/// to the standalone inventory microservice.
/// </summary>
public interface IInventoryServiceClient
{
    /// <summary>Retrieves all inventory items.</summary>
    Task<List<InventoryItem>> GetAllInventoryAsync();

    /// <summary>Retrieves the inventory record for a specific product.</summary>
    /// <param name="productId">The product identifier.</param>
    Task<InventoryItem?> GetInventoryByProductIdAsync(int productId);

    /// <summary>Restocks an inventory item by adding the specified quantity.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The number of units to add.</param>
    Task<InventoryItem> RestockAsync(int productId, int quantity);

    /// <summary>Deducts stock from a product's inventory.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The number of units to deduct.</param>
    Task<InventoryItem> DeductStockAsync(int productId, int quantity);

    /// <summary>Retrieves inventory items at or below the reorder threshold.</summary>
    Task<List<InventoryItem>> GetLowStockItemsAsync();
}
