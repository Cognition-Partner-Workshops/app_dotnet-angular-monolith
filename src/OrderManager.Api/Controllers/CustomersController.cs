using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.Models;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

/// <summary>
/// REST API controller for managing customer resources.
/// Provides endpoints for listing, retrieving, and creating customers.
/// Route: api/customers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    /// <summary>
    /// The customer service used to perform business logic operations.
    /// </summary>
    private readonly CustomerService _customerService;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomersController"/> with the specified service.
    /// </summary>
    /// <param name="customerService">The customer service injected via dependency injection.</param>
    public CustomersController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Retrieves all customers.
    /// </summary>
    /// <returns>An HTTP 200 response containing a list of all customers.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _customerService.GetAllCustomersAsync());

    /// <summary>
    /// Retrieves a single customer by their unique identifier, including their order history.
    /// </summary>
    /// <param name="id">The unique identifier of the customer.</param>
    /// <returns>An HTTP 200 response with the customer data, or HTTP 404 if the customer is not found.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        return customer is null ? NotFound() : Ok(customer);
    }

    /// <summary>
    /// Creates a new customer record.
    /// </summary>
    /// <param name="customer">The customer data provided in the request body.</param>
    /// <returns>An HTTP 201 response with the created customer and a Location header pointing to the new resource.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Customer customer)
    {
        var created = await _customerService.CreateCustomerAsync(customer);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
