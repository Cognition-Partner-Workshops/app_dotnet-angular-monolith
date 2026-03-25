using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// CRUD operations for <see cref="Customer"/> entities.
/// </summary>
public class CustomerService
{
    private readonly AppDbContext _context;

    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns all customers (without eagerly loading their orders).</summary>
    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        return await _context.Customers.ToListAsync();
    }

    /// <summary>Returns a customer by ID including their order history, or null if not found.</summary>
    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _context.Customers.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>Persists a new customer and returns it with the generated ID.</summary>
    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }
}
