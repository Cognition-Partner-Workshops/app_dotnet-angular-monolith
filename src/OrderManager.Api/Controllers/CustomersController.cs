using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// API controller for managing customer records.
/// Exposes endpoints under <c>/api/customers</c>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;

    /// <summary>
    /// Initializes the controller with the required customer service.
    /// </summary>
    /// <param name="customerService">The service handling customer business logic.</param>
    public CustomersController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Retrieves all customers.
    /// </summary>
    /// <returns>200 OK with a list of all customers.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _customerService.GetAllCustomersAsync());

    /// <summary>
    /// Retrieves a single customer by their unique identifier, including associated orders.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <returns>200 OK with the customer, or 404 Not Found.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        return customer is null ? NotFound() : Ok(customer);
    }

    /// <summary>
    /// Creates a new customer record.
    /// </summary>
    /// <param name="customer">The customer data to create.</param>
    /// <returns>201 Created with the new customer and a Location header pointing to the new resource.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Customer customer)
    {
        var created = await _customerService.CreateCustomerAsync(customer);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
