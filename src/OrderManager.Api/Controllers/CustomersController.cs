using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// API controller for managing <see cref="Customer"/> resources.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;

    /// <summary>
    /// Initializes the controller with the customer service dependency.
    /// </summary>
    public CustomersController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Returns all customers.
    /// </summary>
    /// <returns>200 OK with a list of customers.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _customerService.GetAllCustomersAsync());

    /// <summary>
    /// Returns a single customer by ID, including their orders.
    /// </summary>
    /// <param name="id">The customer's unique identifier.</param>
    /// <returns>200 OK with the customer, or 404 Not Found.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        return customer is null ? NotFound() : Ok(customer);
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="customer">The customer data to persist.</param>
    /// <returns>201 Created with a Location header pointing to the new resource.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Customer customer)
    {
        var created = await _customerService.CreateCustomerAsync(customer);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
