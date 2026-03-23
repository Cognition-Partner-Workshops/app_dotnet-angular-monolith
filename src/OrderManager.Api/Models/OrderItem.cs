namespace OrderManager.Api.Models;

/// <summary>
/// Represents a single line item within an order.
/// Each order item links an order to a product with a specified quantity and unit price.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the order item.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key referencing the parent order.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Gets or sets the parent order that contains this line item.
    /// Navigation property for the many-to-one relationship with <see cref="Order"/>.
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Gets or sets the foreign key referencing the product in this line item.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product associated with this line item.
    /// Navigation property for the many-to-one relationship with <see cref="Product"/>.
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of units of the product ordered.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the price per unit at the time the order was placed.
    /// This captures the historical price and may differ from the current product price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets the computed total for this line item, calculated as <see cref="Quantity"/> multiplied by <see cref="UnitPrice"/>.
    /// This is a read-only computed property and is not persisted in the database.
    /// </summary>
    public decimal LineTotal => Quantity * UnitPrice;
}
