using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

public interface IInventoryServiceClient
{
    Task<List<InventoryItem>> GetAllInventoryAsync();
    Task<InventoryItem?> GetInventoryByProductIdAsync(int productId);
    Task<InventoryItem> RestockAsync(int productId, int quantity);
    Task<InventoryItem> DeductStockAsync(int productId, int quantity);
    Task<List<InventoryItem>> GetLowStockItemsAsync();
}
