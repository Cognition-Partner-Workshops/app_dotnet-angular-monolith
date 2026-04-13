using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// API controller for managing orders and their lifecycle status.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    /// <summary>
    /// Initializes a new instance of <see cref="OrdersController"/>.
    /// </summary>
    /// <param name="orderService">The service handling order business logic.</param>
    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Returns all orders sorted by date descending, including customer and line-item details.
    /// </summary>
    /// <returns>A 200 OK response containing a list of all orders.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _orderService.GetAllOrdersAsync());

    /// <summary>
    /// Returns a single order by its identifier, including customer and line-item details.
    /// </summary>
    /// <param name="id">The unique identifier of the order.</param>
    /// <returns>A 200 OK response with the order, or 404 Not Found if the order does not exist.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>
    /// Creates a new order for the specified customer, validating product availability and decrementing inventory.
    /// </summary>
    /// <param name="request">The order creation payload containing the customer ID and line items.</param>
    /// <returns>A 201 Created response with the new order and a Location header pointing to the new resource.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var items = request.Items.Select(i => (i.ProductId, i.Quantity)).ToList();
        var order = await _orderService.CreateOrderAsync(request.CustomerId, items);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Updates the status of an existing order (e.g. from "Pending" to "Shipped").
    /// </summary>
    /// <param name="id">The unique identifier of the order to update.</param>
    /// <param name="request">The payload containing the new status value.</param>
    /// <returns>A 200 OK response with the updated order.</returns>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var order = await _orderService.UpdateOrderStatusAsync(id, request.Status);
        return Ok(order);
    }
}

/// <summary>Request payload for creating a new order.</summary>
/// <param name="CustomerId">The identifier of the customer placing the order.</param>
/// <param name="Items">The list of products and quantities to include in the order.</param>
public record CreateOrderRequest(int CustomerId, List<OrderItemRequest> Items);

/// <summary>Represents a single line item in a <see cref="CreateOrderRequest"/>.</summary>
/// <param name="ProductId">The identifier of the product to order.</param>
/// <param name="Quantity">The number of units to order.</param>
public record OrderItemRequest(int ProductId, int Quantity);

/// <summary>Request payload for updating an order's status.</summary>
/// <param name="Status">The new status value (e.g. "Shipped", "Delivered", "Cancelled").</param>
public record UpdateStatusRequest(string Status);
