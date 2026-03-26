using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Orchestrates order creation, status updates, and stock deduction
/// by coordinating between the local database and the inventory microservice.
/// </summary>
public class OrderService
{
    private readonly AppDbContext _context;
    private readonly InventoryApiClient _inventoryClient;

    /// <summary>
    /// Initializes a new instance of <see cref="OrderService"/>.
    /// </summary>
    /// <param name="context">The EF Core database context for orders, products, and customers.</param>
    /// <param name="inventoryClient">HTTP client for the inventory microservice.</param>
    public OrderService(AppDbContext context, InventoryApiClient inventoryClient)
    {
        _context = context;
        _inventoryClient = inventoryClient;
    }

    /// <summary>Retrieves all orders with their associated customer and line items.</summary>
    /// <returns>Orders sorted by date descending.</returns>
    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    /// <summary>Retrieves a single order by its identifier.</summary>
    /// <param name="id">The order identifier.</param>
    /// <returns>The order with customer and items, or <c>null</c> if not found.</returns>
    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>Creates a new order, deducting stock via the inventory microservice before persisting.</summary>
    /// <param name="customerId">The customer placing the order.</param>
    /// <param name="items">Product/quantity pairs for the order line items.</param>
    /// <returns>The newly created order.</returns>
    /// <exception cref="ArgumentException">Thrown when customer or product is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stock deduction fails.</exception>
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

            // Deduct stock via the inventory microservice
            var inventory = await _inventoryClient.DeductStockAsync(productId, quantity)
                ?? throw new InvalidOperationException($"No inventory record for product {productId}");

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

    /// <summary>Updates the status of an existing order.</summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="status">The new status value.</param>
    /// <returns>The updated order.</returns>
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
