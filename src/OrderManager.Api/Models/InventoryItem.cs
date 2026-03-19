namespace OrderManager.Api.Models;

/// <summary>
/// Tracks the stock level and warehouse location for a single <see cref="Product"/>.
/// </summary>
/// <remarks>
/// Each product has exactly one inventory record (one-to-one relationship).
/// Items are flagged as low-stock when <see cref="QuantityOnHand"/> falls at or below
/// <see cref="ReorderLevel"/>.
/// </remarks>
public class InventoryItem
{
    /// <summary>Gets or sets the unique identifier for the inventory record.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key referencing the associated <see cref="Product"/>.</summary>
    public int ProductId { get; set; }

    /// <summary>Gets or sets the product this inventory record tracks.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Gets or sets the current number of units in stock.</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>Gets or sets the stock threshold below which the item is considered low-stock. Defaults to 10.</summary>
    public int ReorderLevel { get; set; } = 10;

    /// <summary>Gets or sets the warehouse aisle/bin location code (e.g., "A-01").</summary>
    public string WarehouseLocation { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp of the most recent restock operation.</summary>
    public DateTime LastRestocked { get; set; } = DateTime.UtcNow;
}
