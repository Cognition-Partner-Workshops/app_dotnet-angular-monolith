using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// REST endpoints for managing customer orders.
/// Supports listing, retrieval, creation, and status updates.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>GET /api/orders — returns all orders, newest first.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _orderService.GetAllOrdersAsync());

    /// <summary>GET /api/orders/{id} — returns a single order or 404.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>
    /// POST /api/orders — creates a new order, validates inventory, and returns 201.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var items = request.Items.Select(i => (i.ProductId, i.Quantity)).ToList();
        var order = await _orderService.CreateOrderAsync(request.CustomerId, items);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>PATCH /api/orders/{id}/status — transitions an order to a new status.</summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var order = await _orderService.UpdateOrderStatusAsync(id, request.Status);
        return Ok(order);
    }
}

// ---- Request DTOs (records keep these concise) ----------------------------

/// <summary>Request body for creating a new order.</summary>
public record CreateOrderRequest(int CustomerId, List<OrderItemRequest> Items);

/// <summary>A single line item within a <see cref="CreateOrderRequest"/>.</summary>
public record OrderItemRequest(int ProductId, int Quantity);

/// <summary>Request body for updating an order's status.</summary>
public record UpdateStatusRequest(string Status);
