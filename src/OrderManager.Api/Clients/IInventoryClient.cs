namespace OrderManager.Api.Clients;

/// <summary>
/// Interface for communicating with the standalone inventory-service microservice.
/// Replaces the former in-process InventoryService that accessed the shared database directly.
/// </summary>
public interface IInventoryClient
{
    Task<List<InventoryItemDto>> GetAllInventoryAsync();
    Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId);
    Task<InventoryItemDto> RestockAsync(int productId, int quantity);
    Task<List<InventoryItemDto>> GetLowStockItemsAsync();
    Task<bool> CheckStockAsync(int productId, int quantity);
    Task<InventoryItemDto> DeductStockAsync(int productId, int quantity);
}
