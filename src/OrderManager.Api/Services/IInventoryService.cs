namespace OrderManager.Api.Services;

/// <summary>
/// Abstraction over inventory operations for testability.
/// Implemented by InventoryService (HTTP client to inventory-service microservice).
/// </summary>
public interface IInventoryService
{
    Task<List<InventoryItemDto>> GetAllInventoryAsync();
    Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId);
    Task<InventoryItemDto> RestockAsync(int productId, int quantity);
    Task<List<InventoryItemDto>> GetLowStockItemsAsync();
    Task<bool> CheckStockAsync(int productId, int quantity);
    Task<InventoryItemDto> DeductStockAsync(int productId, int quantity);
}
