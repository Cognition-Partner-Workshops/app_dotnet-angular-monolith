namespace OrderManager.Api.Models;

/// <summary>
/// Tracks warehouse stock for a single <see cref="Product"/>.
/// Items whose <see cref="QuantityOnHand"/> falls at or below <see cref="ReorderLevel"/>
/// are flagged as "low stock" in the UI and the low-stock API endpoint.
/// </summary>
public class InventoryItem
{
    public int Id { get; set; }

    /// <summary>Foreign key to the <see cref="Product"/> this record tracks.</summary>
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Current physical count in the warehouse.</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>Threshold at or below which the item is considered low stock (default 10).</summary>
    public int ReorderLevel { get; set; } = 10;

    /// <summary>Physical location code within the warehouse (e.g. "A-01").</summary>
    public string WarehouseLocation { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the most recent restock event.</summary>
    public DateTime LastRestocked { get; set; } = DateTime.UtcNow;
}
