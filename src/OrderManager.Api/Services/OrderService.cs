using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Provides business logic for the order lifecycle: creation, retrieval, and status management.
/// </summary>
/// <remarks>
/// Order creation is transactional—it validates the customer, checks inventory for every
/// requested line item, deducts stock, snapshots the unit price, and computes the order total
/// before persisting.
/// </remarks>
public class OrderService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="OrderService"/>.
    /// </summary>
    /// <param name="context">The database context for data access.</param>
    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all orders with their customer and line-item details, sorted newest first.
    /// </summary>
    /// <returns>A list of <see cref="Order"/> records with customer and items eagerly loaded.</returns>
    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a single order by ID with full details.
    /// </summary>
    /// <param name="id">The order's unique identifier.</param>
    /// <returns>The matching <see cref="Order"/> with customer and items, or <c>null</c> if not found.</returns>
    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>
    /// Creates a new order for the specified customer, validating inventory and deducting stock.
    /// </summary>
    /// <param name="customerId">The ID of the customer placing the order.</param>
    /// <param name="items">A list of (ProductId, Quantity) tuples representing the desired line items.</param>
    /// <returns>The newly created <see cref="Order"/> with computed totals.</returns>
    /// <exception cref="ArgumentException">Thrown when the customer or a product is not found.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a product has no inventory record or insufficient stock.
    /// </exception>
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
    /// Updates the status of an existing order (e.g., "Pending" to "Shipped").
    /// </summary>
    /// <param name="orderId">The order's unique identifier.</param>
    /// <param name="status">The new status value.</param>
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
