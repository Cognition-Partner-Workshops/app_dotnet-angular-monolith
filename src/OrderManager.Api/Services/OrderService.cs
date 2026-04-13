using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Provides business logic for managing orders, including creation with inventory validation
/// and status lifecycle management.
/// </summary>
public class OrderService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="OrderService"/>.
    /// </summary>
    /// <param name="context">The database context used for order data access.</param>
    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all orders sorted by date descending, including customer and line-item details.
    /// </summary>
    /// <returns>A list of all <see cref="Order"/> records with related data eagerly loaded.</returns>
    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a single order by its identifier, including customer and line-item details.
    /// </summary>
    /// <param name="id">The unique identifier of the order.</param>
    /// <returns>The matching <see cref="Order"/> with related data loaded, or <c>null</c> if not found.</returns>
    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>
    /// Creates a new order for the specified customer. Validates that each requested product exists
    /// and has sufficient inventory, then decrements stock for each line item.
    /// The shipping address is automatically derived from the customer's address on file.
    /// </summary>
    /// <param name="customerId">The identifier of the customer placing the order.</param>
    /// <param name="items">A list of (ProductId, Quantity) tuples representing the order line items.</param>
    /// <returns>The newly created <see cref="Order"/> with its generated identifier and computed total.</returns>
    /// <exception cref="ArgumentException">Thrown when the customer or a product is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a product has no inventory record or insufficient stock.</exception>
    public async Task<Order> CreateOrderAsync(int customerId, List<(int ProductId, int Quantity)> items)
    {
        var customer = await _context.Customers.FindAsync(customerId)
            ?? throw new ArgumentException($"Customer {customerId} not found");

        var order = new Order
        {
            CustomerId = customerId,
            ShippingAddress = $"{customer.Address}, {customer.City}, {customer.State} {customer.ZipCode}"
        };

        foreach (var (productId, quantity) in items)
        {
            var product = await _context.Products.FindAsync(productId)
                ?? throw new ArgumentException($"Product {productId} not found");

            var inventory = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId)
                ?? throw new InvalidOperationException($"No inventory record for product {productId}");

            if (inventory.QuantityOnHand < quantity)
                throw new InvalidOperationException($"Insufficient stock for {product.Name}. Available: {inventory.QuantityOnHand}");

            inventory.QuantityOnHand -= quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    /// <summary>
    /// Updates the status of an existing order (e.g. from "Pending" to "Shipped").
    /// </summary>
    /// <param name="orderId">The identifier of the order to update.</param>
    /// <param name="status">The new status value to set.</param>
    /// <returns>The updated <see cref="Order"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the order is not found.</exception>
    public async Task<Order> UpdateOrderStatusAsync(int orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId)
            ?? throw new ArgumentException($"Order {orderId} not found");
        order.Status = status;
        await _context.SaveChangesAsync();
        return order;
    }
}
