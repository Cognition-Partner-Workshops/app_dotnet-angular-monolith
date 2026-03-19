namespace OrderManager.Api.Models;

/// <summary>
/// Represents a single line item within an <see cref="Order"/>, linking to a specific <see cref="Product"/>.
/// </summary>
/// <remarks>
/// The <see cref="UnitPrice"/> is captured at order creation time (snapshotted from the product's
/// current price) so that subsequent price changes do not affect existing orders.
/// </remarks>
public class OrderItem
{
    /// <summary>Gets or sets the unique identifier for the order item.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key referencing the parent <see cref="Order"/>.</summary>
    public int OrderId { get; set; }

    /// <summary>Gets or sets the parent order this line item belongs to.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>Gets or sets the foreign key referencing the <see cref="Product"/> being ordered.</summary>
    public int ProductId { get; set; }

    /// <summary>Gets or sets the product associated with this line item.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Gets or sets the number of units ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the price per unit at the time the order was placed.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets the computed total for this line item (<see cref="Quantity"/> x <see cref="UnitPrice"/>).</summary>
    public decimal LineTotal => Quantity * UnitPrice;
}
