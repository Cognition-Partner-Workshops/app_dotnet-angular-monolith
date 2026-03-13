using Microsoft.AspNetCore.Mvc;
using CustomerService.Api.Models;
using CustomerService.Api.Services;

namespace CustomerService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CustomersService _customersService;

    public CustomersController(CustomersService customersService)
    {
        _customersService = customersService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _customersService.GetAllCustomersAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customersService.GetCustomerByIdAsync(id);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Customer customer)
    {
        var created = await _customersService.CreateCustomerAsync(customer);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Customer customer)
    {
        var updated = await _customersService.UpdateCustomerAsync(id, customer);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _customersService.DeleteCustomerAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
