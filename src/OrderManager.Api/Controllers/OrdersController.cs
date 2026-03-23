using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// REST API controller for managing customer orders.
/// Provides endpoints for listing, retrieving, creating orders, and updating order status.
/// Route: api/orders
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    /// <summary>
    /// The order service used to perform business logic operations.
    /// </summary>
    private readonly OrderService _orderService;

    /// <summary>
    /// Initializes a new instance of <see cref="OrdersController"/> with the specified service.
    /// </summary>
    /// <param name="orderService">The order service injected via dependency injection.</param>
    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Retrieves all orders sorted by date (most recent first), including customer and line item details.
    /// </summary>
    /// <returns>An HTTP 200 response containing a list of all orders.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _orderService.GetAllOrdersAsync());

    /// <summary>
    /// Retrieves a single order by its unique identifier, including customer and line item details.
    /// </summary>
    /// <param name="id">The unique identifier of the order.</param>
    /// <returns>An HTTP 200 response with the order data, or HTTP 404 if the order is not found.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>
    /// Creates a new order for a customer with the specified line items.
    /// Validates inventory availability and deducts stock for each item.
    /// </summary>
    /// <param name="request">The order creation request containing the customer ID and list of items.</param>
    /// <returns>An HTTP 201 response with the created order and a Location header pointing to the new resource.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var items = request.Items.Select(i => (i.ProductId, i.Quantity)).ToList();
        var order = await _orderService.CreateOrderAsync(request.CustomerId, items);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Updates the status of an existing order (e.g., "Pending" to "Shipped").
    /// </summary>
    /// <param name="id">The unique identifier of the order to update.</param>
    /// <param name="request">The request containing the new status value.</param>
    /// <returns>An HTTP 200 response with the updated order.</returns>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var order = await _orderService.UpdateOrderStatusAsync(id, request.Status);
        return Ok(order);
    }
}

/// <summary>
/// Request model for creating a new order.
/// </summary>
/// <param name="CustomerId">The unique identifier of the customer placing the order.</param>
/// <param name="Items">The list of order line items specifying products and quantities.</param>
public record CreateOrderRequest(int CustomerId, List<OrderItemRequest> Items);

/// <summary>
/// Request model for a single line item in an order creation request.
/// </summary>
/// <param name="ProductId">The unique identifier of the product to order.</param>
/// <param name="Quantity">The number of units to order.</param>
public record OrderItemRequest(int ProductId, int Quantity);

/// <summary>
/// Request model for updating the status of an existing order.
/// </summary>
/// <param name="Status">The new status value (e.g., "Shipped", "Delivered", "Cancelled").</param>
public record UpdateStatusRequest(string Status);
