namespace OrderManager.Api.Models;

/// <summary>
/// Represents the inventory record for a single product, tracking stock levels and warehouse location.
/// Each product has at most one associated inventory item (one-to-one relationship).
/// </summary>
public class InventoryItem
{
    /// <summary>Gets or sets the unique identifier for this inventory record.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key to the product this inventory record tracks.</summary>
    public int ProductId { get; set; }

    /// <summary>Gets or sets the navigation property to the associated product.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Gets or sets the current quantity of the product in stock.</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>Gets or sets the minimum stock threshold that triggers a low-stock warning. Defaults to 10.</summary>
    public int ReorderLevel { get; set; } = 10;

    /// <summary>Gets or sets the warehouse aisle/bin location identifier (e.g. "A-01").</summary>
    public string WarehouseLocation { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp of the most recent restock event.</summary>
    public DateTime LastRestocked { get; set; } = DateTime.UtcNow;
}
