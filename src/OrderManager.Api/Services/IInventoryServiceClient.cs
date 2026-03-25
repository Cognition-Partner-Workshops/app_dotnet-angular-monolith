using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Interface for inventory service client, enabling testability.
/// The concrete implementation (InventoryHttpClient) makes HTTP calls
/// to the standalone inventory microservice.
/// </summary>
public interface IInventoryServiceClient
{
    Task<List<InventoryItem>> GetAllInventoryAsync();
    Task<InventoryItem?> GetInventoryByProductIdAsync(int productId);
    Task<InventoryItem> RestockAsync(int productId, int quantity);
    Task<InventoryItem> DeductStockAsync(int productId, int quantity);
    Task<List<InventoryItem>> GetLowStockItemsAsync();
}
