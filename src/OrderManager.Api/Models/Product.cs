namespace OrderManager.Api.Models;

/// <summary>
/// A product in the catalog. Each product has a unique SKU and an optional
/// one-to-one relationship with an <see cref="InventoryItem"/> for stock tracking.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Grouping label used to filter products (e.g. "Widgets", "Gadgets").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Current list price stored with two-decimal precision.</summary>
    public decimal Price { get; set; }

    /// <summary>Stock-keeping unit — unique across the catalog.</summary>
    public string Sku { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Associated warehouse inventory record (one-to-one, nullable if not yet stocked).</summary>
    public InventoryItem? Inventory { get; set; }

    /// <summary>All order line items that reference this product.</summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
