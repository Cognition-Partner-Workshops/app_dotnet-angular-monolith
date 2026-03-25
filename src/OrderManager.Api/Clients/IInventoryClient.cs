namespace OrderManager.Api.Clients;

public interface IInventoryClient
{
    Task<List<InventoryItemDto>> GetAllInventoryAsync();
    Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId);
    Task<InventoryItemDto> RestockAsync(int productId, int quantity);
    Task<List<InventoryItemDto>> GetLowStockItemsAsync();
    Task<bool> CheckStockAsync(int productId, int quantity);
    Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity);
}
