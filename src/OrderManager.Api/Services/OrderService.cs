using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Handles order creation, retrieval, and status updates.
/// Order creation validates stock availability and atomically decrements inventory.
/// </summary>
public class OrderService
{
    private readonly AppDbContext _context;

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns all orders with their customer and line-item details, newest first.</summary>
    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    /// <summary>Returns a single order by ID with full details, or null if not found.</summary>
    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>
    /// Creates a new order for the given customer with the specified line items.
    /// For each item the method validates stock, decrements inventory, and snapshots
    /// the current product price. The entire operation is saved in a single
    /// <see cref="AppDbContext.SaveChangesAsync"/> call so inventory and order
    /// updates are atomic.
    /// </summary>
    /// <param name="customerId">ID of the customer placing the order.</param>
    /// <param name="items">List of (ProductId, Quantity) tuples to order.</param>
    /// <returns>The newly created <see cref="Order"/> with computed totals.</returns>
    /// <exception cref="ArgumentException">Customer or product not found.</exception>
    /// <exception cref="InvalidOperationException">Insufficient stock or missing inventory record.</exception>
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
    /// Transitions an order to the given status (e.g. Processing, Shipped, Delivered).
    /// </summary>
    /// <exception cref="ArgumentException">Order not found.</exception>
    public async Task<Order> UpdateOrderStatusAsync(int orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId)
            ?? throw new ArgumentException($"Order {orderId} not found");
        order.Status = status;
        await _context.SaveChangesAsync();
        return order;
    }
}
