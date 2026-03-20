using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Provides business logic for the order lifecycle: creation, retrieval, and status updates.
/// Handles inventory validation and deduction during order creation.
/// </summary>
public class OrderService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="OrderService"/>.
    /// </summary>
    /// <param name="context">The database context used for data access.</param>
    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all orders with customer and line item details, sorted by most recent first.
    /// </summary>
    /// <returns>A list of all <see cref="Order"/> records with eagerly loaded relationships.</returns>
    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a single order by ID, including customer and line item details.
    /// </summary>
    /// <param name="id">The unique identifier of the order.</param>
    /// <returns>The matching <see cref="Order"/> with relationships, or <c>null</c> if not found.</returns>
    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>
    /// Creates a new order for the specified customer, validating and deducting inventory for each item.
    /// The shipping address is derived from the customer's stored address.
    /// </summary>
    /// <param name="customerId">The ID of the customer placing the order.</param>
    /// <param name="items">A list of (ProductId, Quantity) tuples representing the line items.</param>
    /// <returns>The newly created <see cref="Order"/> with computed total amount.</returns>
    /// <exception cref="ArgumentException">Thrown if the customer or any product is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if inventory is missing or insufficient for any item.</exception>
    public async Task<Order> CreateOrderAsync(int customerId, List<(int ProductId, int Quantity)> items)
    {
        var customer = await _context.Customers.FindAsync(customerId)
            ?? throw new ArgumentException($"Customer {customerId} not found");

        var order = new Order
        {
            CustomerId = customerId,
            ShippingAddress = $"{customer.Address}, {customer.City}, {customer.State} {customer.ZipCode}"
        };

        // Validate stock and build line items for each requested product
        foreach (var (productId, quantity) in items)
        {
            var product = await _context.Products.FindAsync(productId)
                ?? throw new ArgumentException($"Product {productId} not found");

            var inventory = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId)
                ?? throw new InvalidOperationException($"No inventory record for product {productId}");

            if (inventory.QuantityOnHand < quantity)
                throw new InvalidOperationException($"Insufficient stock for {product.Name}. Available: {inventory.QuantityOnHand}");

            // Deduct stock for the ordered quantity
            inventory.QuantityOnHand -= quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price
            });
        }

        // Compute the order total from all line items
        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    /// <summary>
    /// Updates the status of an existing order (e.g., "Pending" to "Shipped").
    /// </summary>
    /// <param name="orderId">The ID of the order to update.</param>
    /// <param name="status">The new status value.</param>
    /// <returns>The updated <see cref="Order"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the order is not found.</exception>
    public async Task<Order> UpdateOrderStatusAsync(int orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId)
            ?? throw new ArgumentException($"Order {orderId} not found");
        order.Status = status;
        await _context.SaveChangesAsync();
        return order;
    }
}
