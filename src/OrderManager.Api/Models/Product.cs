namespace OrderManager.Api.Models;

/// <summary>
/// Represents a product available for sale in the OrderManager catalog.
/// Each product has a unique SKU and can be associated with inventory tracking and order line items.
/// </summary>
public class Product
{
    /// <summary>
    /// Gets or sets the unique identifier for the product.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the product.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a detailed description of the product.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product category used for grouping and filtering (e.g., "Widgets", "Gadgets").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unit price of the product in decimal currency.
    /// Stored with precision decimal(18,2) in the database.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the Stock Keeping Unit (SKU) code. Must be unique across all products.
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the product was created.
    /// Defaults to the current UTC time on creation.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the associated inventory record for this product.
    /// Navigation property for the one-to-one relationship with <see cref="InventoryItem"/>.
    /// May be null if no inventory record exists yet.
    /// </summary>
    public InventoryItem? Inventory { get; set; }

    /// <summary>
    /// Gets or sets the collection of order line items that reference this product.
    /// Navigation property for the one-to-many relationship with <see cref="OrderItem"/>.
    /// </summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
