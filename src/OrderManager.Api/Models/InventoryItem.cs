namespace OrderManager.Api.Models;

/// <summary>
/// Represents an inventory record tracking stock levels for a specific product.
/// Each inventory item has a one-to-one relationship with a <see cref="Product"/>.
/// </summary>
public class InventoryItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the inventory record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key referencing the associated product.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product associated with this inventory record.
    /// Navigation property for the one-to-one relationship with <see cref="Product"/>.
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current quantity of the product available in the warehouse.
    /// </summary>
    public int QuantityOnHand { get; set; }

    /// <summary>
    /// Gets or sets the minimum stock threshold that triggers a reorder alert.
    /// When <see cref="QuantityOnHand"/> falls to or below this level, the item is flagged as low stock.
    /// Defaults to 10 units.
    /// </summary>
    public int ReorderLevel { get; set; } = 10;

    /// <summary>
    /// Gets or sets the physical warehouse location identifier (e.g., "A-01", "B-12").
    /// </summary>
    public string WarehouseLocation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp of the last restock event for this item.
    /// Defaults to the current UTC time on creation.
    /// </summary>
    public DateTime LastRestocked { get; set; } = DateTime.UtcNow;
}
