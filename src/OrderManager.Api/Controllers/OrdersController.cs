using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// API controller for the order lifecycle: creation, retrieval, and status updates.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    /// <summary>
    /// Initializes the controller with the order service dependency.
    /// </summary>
    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Returns all orders sorted by date descending, with customer and line-item details.
    /// </summary>
    /// <returns>200 OK with a list of orders.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _orderService.GetAllOrdersAsync());

    /// <summary>
    /// Returns a single order by ID with full details.
    /// </summary>
    /// <param name="id">The order's unique identifier.</param>
    /// <returns>200 OK with the order, or 404 Not Found.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>
    /// Creates a new order, validates inventory, deducts stock, and computes totals.
    /// </summary>
    /// <param name="request">The customer ID and list of product/quantity pairs.</param>
    /// <returns>201 Created with a Location header pointing to the new order.</returns>
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
    /// <param name="id">The order's unique identifier.</param>
    /// <param name="request">The new status value.</param>
    /// <returns>200 OK with the updated order.</returns>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var order = await _orderService.UpdateOrderStatusAsync(id, request.Status);
        return Ok(order);
    }
}

/// <summary>Request body for creating a new order.</summary>
/// <param name="CustomerId">The ID of the customer placing the order.</param>
/// <param name="Items">The list of products and quantities to include in the order.</param>
public record CreateOrderRequest(int CustomerId, List<OrderItemRequest> Items);

/// <summary>Represents a single product/quantity pair within an order creation request.</summary>
/// <param name="ProductId">The product to order.</param>
/// <param name="Quantity">The number of units to order.</param>
public record OrderItemRequest(int ProductId, int Quantity);

/// <summary>Request body for updating an order's status.</summary>
/// <param name="Status">The new status value (e.g., "Shipped", "Delivered", "Cancelled").</param>
public record UpdateStatusRequest(string Status);
