namespace OrderManager.Api.Models;

/// <summary>
/// Represents a product available for sale in the catalog.
/// </summary>
/// <remarks>
/// Each product has a unique SKU and is linked one-to-one with an <see cref="InventoryItem"/>
/// that tracks its stock level. Products appear as line items in orders via <see cref="OrderItem"/>.
/// </remarks>
public class Product
{
    /// <summary>Gets or sets the unique identifier for the product.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the display name of the product.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a brief description of the product.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the product category (e.g., "Widgets", "Gadgets").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the unit price. Stored as decimal(18,2) in the database.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the stock-keeping unit code. Must be unique across all products.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the product was added to the catalog.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the associated inventory record tracking stock levels for this product.</summary>
    public InventoryItem? Inventory { get; set; }

    /// <summary>Gets or sets the collection of order line items that reference this product.</summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
