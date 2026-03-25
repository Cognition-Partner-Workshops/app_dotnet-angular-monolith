using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// REST endpoints for managing customer records.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;

    public CustomersController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>GET /api/customers — returns all customers.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _customerService.GetAllCustomersAsync());

    /// <summary>GET /api/customers/{id} — returns a single customer with order history, or 404.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        return customer is null ? NotFound() : Ok(customer);
    }

    /// <summary>POST /api/customers — creates a new customer record and returns 201.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Customer customer)
    {
        var created = await _customerService.CreateCustomerAsync(customer);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
