using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Orchestrates order creation, status updates, and stock reservation/deduction
/// by coordinating between the local database and the inventory microservice.
/// </summary>
public class OrderService
{
    private readonly AppDbContext _context;
    private readonly InventoryApiClient _inventoryApiClient;
    private readonly ILogger<OrderService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="OrderService"/>.
    /// </summary>
    /// <param name="context">The EF Core database context for orders, products, and customers.</param>
    /// <param name="inventoryApiClient">HTTP client for the inventory microservice.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public OrderService(AppDbContext context, InventoryApiClient inventoryApiClient, ILogger<OrderService> logger)
    {
        _context = context;
        _inventoryApiClient = inventoryApiClient;
        _logger = logger;
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order> CreateOrderAsync(int customerId, List<(int ProductId, int Quantity)> items)
    {
        var customer = await _context.Customers.FindAsync(customerId)
            ?? throw new ArgumentException($"Customer {customerId} not found");

        // Reserve stock atomically via the inventory microservice
        var reservationRequest = new StockReservationRequest
        {
            Items = items.Select(i => new StockReservationItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };

        var reservationResult = await _inventoryApiClient.CheckAndReserveStockAsync(reservationRequest);
        if (!reservationResult.Success)
        {
            throw new InvalidOperationException($"Stock reservation failed: {reservationResult.Message}");
        }

        var order = new Order
        {
            CustomerId = customerId,
            ShippingAddress = $"{customer.Address}, {customer.City}, {customer.State} {customer.ZipCode}"
        };

        // Check stock availability via inventory-service before creating order
        foreach (var (productId, quantity) in items)
        {
            var product = await _context.Products.FindAsync(productId)
                ?? throw new ArgumentException($"Product {productId} not found");

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

        // Deduct stock via inventory-service after order is persisted
        foreach (var item in order.Items)
        {
            await _inventoryApiClient.DeductStockAsync(item.ProductId, item.Quantity);
            _logger.LogInformation("Deducted {Quantity} units of product {ProductId} via inventory service", item.Quantity, item.ProductId);
        }

        return order;
    }

    public async Task<Order> UpdateOrderStatusAsync(int orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId)
            ?? throw new ArgumentException($"Order {orderId} not found");
        order.Status = status;
        await _context.SaveChangesAsync();
        return order;
    }
}
