namespace OrderManager.Api.Services;

public interface IInventoryServiceClient
{
    Task<List<InventoryDto>> GetAllInventoryAsync();
    Task<InventoryDto?> GetInventoryByProductIdAsync(int productId);
    Task<InventoryDto> RestockAsync(int productId, int quantity);
    Task<InventoryDto> DeductStockAsync(int productId, int quantity);
    Task<List<InventoryDto>> GetLowStockItemsAsync();
}

public class InventoryDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; }
}
