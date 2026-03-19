namespace OrderManager.Api.Models;

/// <summary>
/// Represents a customer order containing one or more line items.
/// </summary>
/// <remarks>
/// Orders are created via <see cref="Services.OrderService.CreateOrderAsync"/> which validates
/// inventory availability, deducts stock, and computes the total amount. Status defaults to
/// "Pending" and can be updated independently via the PATCH endpoint.
/// </remarks>
public class Order
{
    /// <summary>Gets or sets the unique identifier for the order.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key referencing the <see cref="Customer"/> who placed this order.</summary>
    public int CustomerId { get; set; }

    /// <summary>Gets or sets the customer who placed this order.</summary>
    public Customer Customer { get; set; } = null!;

    /// <summary>Gets or sets the UTC date and time when the order was placed.</summary>
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the current status of the order (e.g., "Pending", "Shipped", "Delivered").</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Gets or sets the total monetary amount for all line items. Stored as decimal(18,2).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Gets or sets the shipping address, auto-populated from the customer's address at order creation.</summary>
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the collection of line items in this order.</summary>
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
