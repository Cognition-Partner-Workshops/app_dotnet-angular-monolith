namespace OrderManager.Api.Services;

public interface IInventoryServiceClient
{
    Task<InventoryStockLevel?> GetStockLevelAsync(int productId);
    Task<bool> DeductStockAsync(int productId, int quantity);
}

public record InventoryStockLevel(int ProductId, int QuantityOnHand);
