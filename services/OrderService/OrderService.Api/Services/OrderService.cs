using Microsoft.EntityFrameworkCore;
using OrderService.Api.Clients;
using OrderService.Api.Data;
using OrderService.Api.Models;

namespace OrderService.Api.Services;

public class OrderService
{
    private readonly OrderDbContext _context;
    private readonly ICustomerApiClient _customerClient;
    private readonly IProductApiClient _productClient;
    private readonly IInventoryApiClient _inventoryClient;

    public OrderService(
        OrderDbContext context,
        ICustomerApiClient customerClient,
        IProductApiClient productClient,
        IInventoryApiClient inventoryClient)
    {
        _context = context;
        _customerClient = customerClient;
        _productClient = productClient;
        _inventoryClient = inventoryClient;
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order> CreateOrderAsync(int customerId, List<(int ProductId, int Quantity)> items)
    {
        var customer = await _customerClient.GetCustomerAsync(customerId)
            ?? throw new ArgumentException($"Customer {customerId} not found");

        var addressResult = await _customerClient.GetCustomerAddressAsync(customerId);

        var order = new Order
        {
            CustomerId = customerId,
            ShippingAddress = addressResult?.FullAddress
                ?? $"{customer.Address}, {customer.City}, {customer.State} {customer.ZipCode}"
        };

        var reservedItems = new List<(int ProductId, int Quantity)>();

        try
        {
            foreach (var (productId, quantity) in items)
            {
                var product = await _productClient.GetProductAsync(productId)
                    ?? throw new ArgumentException($"Product {productId} not found");

                var reserveResult = await _inventoryClient.ReserveStockAsync(productId, quantity);
                if (!reserveResult.Success)
                    throw new InvalidOperationException(reserveResult.Message);

                reservedItems.Add((productId, quantity));

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
        catch
        {
            foreach (var (productId, quantity) in reservedItems)
            {
                try
                {
                    await _inventoryClient.ReleaseStockAsync(productId, quantity);
                }
                catch
                {
                    // Log compensation failure in production
                }
            }
            throw;
        }
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
