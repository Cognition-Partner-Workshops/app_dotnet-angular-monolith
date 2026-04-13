namespace OrderManager.Api.Models;

/// <summary>
/// Represents a customer order containing one or more line items.
/// </summary>
public class Order
{
    /// <summary>Gets or sets the unique identifier for the order.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key to the customer who placed this order.</summary>
    public int CustomerId { get; set; }

    /// <summary>Gets or sets the navigation property to the customer who placed this order.</summary>
    public Customer Customer { get; set; } = null!;

    /// <summary>Gets or sets the UTC date and time when the order was placed.</summary>
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the current status of the order (e.g. "Pending", "Shipped", "Delivered").</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Gets or sets the total monetary amount for the order, computed from its line items.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Gets or sets the full shipping address, derived from the customer's address at order time.</summary>
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the collection of line items included in this order.</summary>
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
