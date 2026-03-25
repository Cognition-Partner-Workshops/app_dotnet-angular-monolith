namespace OrderManager.Api.Services;

/// <summary>
/// Interface for inventory service client, enabling testability.
/// The concrete implementation (InventoryServiceClient) makes HTTP calls
/// to the standalone inventory microservice.
/// </summary>
public interface IInventoryServiceClient
{
    Task<List<InventoryItemDto>> GetAllInventoryAsync();
    Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId);
    Task<InventoryItemDto> RestockAsync(int productId, int quantity);
    Task<List<InventoryItemDto>> GetLowStockItemsAsync();
    Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity);
}
