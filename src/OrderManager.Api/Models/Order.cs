namespace OrderManager.Api.Models;

/// <summary>
/// Represents a customer order in the OrderManager system.
/// An order belongs to a single customer and contains one or more order line items.
/// </summary>
public class Order
{
    /// <summary>
    /// Gets or sets the unique identifier for the order.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key referencing the customer who placed the order.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer who placed this order.
    /// Navigation property for the many-to-one relationship with <see cref="Customer"/>.
    /// </summary>
    public Customer Customer { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UTC date and time when the order was placed.
    /// Defaults to the current UTC time on creation.
    /// </summary>
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the current status of the order (e.g., "Pending", "Shipped", "Delivered").
    /// Defaults to "Pending" when a new order is created.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Gets or sets the total monetary amount of the order, calculated as the sum of all line item totals.
    /// Stored with precision decimal(18,2) in the database.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the shipping address for the order, typically derived from the customer's address.
    /// </summary>
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of line items in this order.
    /// Navigation property for the one-to-many relationship with <see cref="OrderItem"/>.
    /// </summary>
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
