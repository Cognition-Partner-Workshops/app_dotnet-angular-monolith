namespace OrderManager.Api.Clients;

public interface IInventoryClient
{
    Task<List<InventoryDto>> GetAllInventoryAsync();
    Task<InventoryDto?> GetInventoryByProductIdAsync(int productId);
    Task<InventoryDto> RestockAsync(int productId, int quantity);
    Task<List<InventoryDto>> GetLowStockItemsAsync();
    Task<bool> CheckStockAsync(int productId, int quantity);
    Task<InventoryDto?> DeductStockAsync(int productId, int quantity);
}

public record InventoryDto(
    int Id,
    int ProductId,
    string ProductName,
    string Sku,
    int QuantityOnHand,
    int ReorderLevel,
    string WarehouseLocation,
    DateTime LastRestocked
);

public record DeductRequest(int Quantity);
