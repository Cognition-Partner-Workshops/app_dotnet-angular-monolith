namespace OrderManager.Api.Models;

/// <summary>
/// DTO for deserializing responses from the Inventory microservice.
/// No longer an EF entity — inventory data is owned by the inventory-service.
/// </summary>
public class InventoryItem
{
    /// <summary>Unique identifier for the inventory record.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key referencing the product in the catalog.</summary>
    public int ProductId { get; set; }

    /// <summary>Denormalized product name for display without cross-service calls.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Product SKU code.</summary>
    public string ProductSku { get; set; } = string.Empty;

    /// <summary>Current quantity available in the warehouse.</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>Threshold below which the item is flagged as low stock.</summary>
    public int ReorderLevel { get; set; } = 10;

    /// <summary>Physical warehouse location code.</summary>
    public string WarehouseLocation { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the most recent restock operation.</summary>
    public DateTime LastRestocked { get; set; } = DateTime.UtcNow;
}
