using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

/// <summary>
/// Provides business logic for managing customer records.
/// </summary>
public class CustomerService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomerService"/>.
    /// </summary>
    /// <param name="context">The database context used for customer data access.</param>
    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all customers in the system.
    /// </summary>
    /// <returns>A list of all <see cref="Customer"/> records.</returns>
    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        return await _context.Customers.ToListAsync();
    }

    /// <summary>
    /// Retrieves a single customer by their identifier, including their associated orders.
    /// </summary>
    /// <param name="id">The unique identifier of the customer.</param>
    /// <returns>The matching <see cref="Customer"/> with orders loaded, or <c>null</c> if not found.</returns>
    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _context.Customers.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Creates a new customer record and persists it to the database.
    /// </summary>
    /// <param name="customer">The customer entity to create.</param>
    /// <returns>The newly created <see cref="Customer"/> with its generated identifier.</returns>
    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }
}
