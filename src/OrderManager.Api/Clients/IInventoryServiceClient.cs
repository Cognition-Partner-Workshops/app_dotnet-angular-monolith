namespace OrderManager.Api.Clients;

/// <summary>
/// DTO for inventory items returned from the inventory microservice.
/// </summary>
public class InventoryItemDto
{
    /// <summary>Unique identifier for the inventory record.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key referencing the product in the catalog.</summary>
    public int ProductId { get; set; }

    /// <summary>Denormalized product name for display without cross-service calls.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Current quantity available in the warehouse.</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>Threshold below which the item is flagged as low stock.</summary>
    public int ReorderLevel { get; set; }

    /// <summary>Physical warehouse location code.</summary>
    public string WarehouseLocation { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the most recent restock operation.</summary>
    public DateTime LastRestocked { get; set; }
}

/// <summary>
/// Result of a stock availability check from the inventory microservice.
/// </summary>
public class StockCheckResult
{
    /// <summary>The product identifier that was checked.</summary>
    public int ProductId { get; set; }

    /// <summary>The quantity that was requested.</summary>
    public int RequestedQuantity { get; set; }

    /// <summary>Whether the requested quantity is available in stock.</summary>
    public bool Available { get; set; }
}

/// <summary>
/// Interface for inventory service client, enabling testability.
/// The concrete implementation makes HTTP calls to the standalone inventory microservice.
/// </summary>
public interface IInventoryServiceClient
{
    /// <summary>Retrieves all inventory items.</summary>
    Task<List<InventoryItemDto>> GetAllInventoryAsync();

    /// <summary>Retrieves the inventory record for a specific product.</summary>
    /// <param name="productId">The product identifier.</param>
    Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId);

    /// <summary>Restocks an inventory item by adding the specified quantity.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The number of units to add.</param>
    Task<InventoryItemDto> RestockAsync(int productId, int quantity);

    /// <summary>Retrieves inventory items at or below the reorder threshold.</summary>
    Task<List<InventoryItemDto>> GetLowStockItemsAsync();

    /// <summary>Checks whether the requested quantity is available for a product.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The quantity to check.</param>
    Task<bool> CheckStockAsync(int productId, int quantity);

    /// <summary>Deducts stock from a product's inventory.</summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The number of units to deduct.</param>
    Task<InventoryItemDto?> DeductStockAsync(int productId, int quantity);
}
