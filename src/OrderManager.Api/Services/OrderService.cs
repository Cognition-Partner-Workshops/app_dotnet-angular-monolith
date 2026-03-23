using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Service responsible for managing order lifecycle operations.
/// Handles order creation with inventory validation, order retrieval, and status updates.
/// Coordinates across Customer, Product, and Inventory entities during order processing.
/// </summary>
public class OrderService
{
    /// <summary>
    /// The database context used for order data access operations.
    /// </summary>
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="OrderService"/> with the specified database context.
    /// </summary>
    /// <param name="context">The application database context for data access.</param>
    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all orders from the database, sorted by order date in descending order.
    /// Eagerly loads associated customer information and order items with their product details.
    /// </summary>
    /// <returns>A list of all <see cref="Order"/> entities with fully loaded navigation properties.</returns>
    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a single order by its unique identifier, including customer and line item details.
    /// </summary>
    /// <param name="id">The unique identifier of the order to retrieve.</param>
    /// <returns>The <see cref="Order"/> with the specified ID and all navigation properties loaded, or null if not found.</returns>
    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>
    /// Creates a new order for the specified customer with the given line items.
    /// Validates that the customer and all products exist, checks inventory availability,
    /// deducts stock for each item, computes the order total, and persists the order.
    /// The shipping address is automatically derived from the customer's address on file.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer placing the order.</param>
    /// <param name="items">A list of tuples containing the product ID and quantity for each line item.</param>
    /// <returns>The newly created <see cref="Order"/> entity with its generated ID and computed total.</returns>
    /// <exception cref="ArgumentException">Thrown when the customer or a product is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no inventory record for a product or insufficient stock.</exception>
    public async Task<Order> CreateOrderAsync(int customerId, List<(int ProductId, int Quantity)> items)
    {
        var customer = await _context.Customers.FindAsync(customerId)
            ?? throw new ArgumentException($"Customer {customerId} not found");

        // Build the order with shipping address derived from customer's address
        var order = new Order
        {
            CustomerId = customerId,
            ShippingAddress = $"{customer.Address}, {customer.City}, {customer.State} {customer.ZipCode}"
        };

        // Validate each line item, check stock availability, and deduct inventory
        foreach (var (productId, quantity) in items)
        {
            var product = await _context.Products.FindAsync(productId)
                ?? throw new ArgumentException($"Product {productId} not found");

            var inventory = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId)
                ?? throw new InvalidOperationException($"No inventory record for product {productId}");

            if (inventory.QuantityOnHand < quantity)
                throw new InvalidOperationException($"Insufficient stock for {product.Name}. Available: {inventory.QuantityOnHand}");

            // Deduct ordered quantity from warehouse stock
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
    /// Updates the status of an existing order (e.g., from "Pending" to "Shipped" or "Delivered").
    /// </summary>
    /// <param name="orderId">The unique identifier of the order to update.</param>
    /// <param name="status">The new status value to set on the order.</param>
    /// <returns>The updated <see cref="Order"/> entity with the new status.</returns>
    /// <exception cref="ArgumentException">Thrown when no order is found with the specified ID.</exception>
    public async Task<Order> UpdateOrderStatusAsync(int orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId)
            ?? throw new ArgumentException($"Order {orderId} not found");
        order.Status = status;
        await _context.SaveChangesAsync();
        return order;
    }
}
