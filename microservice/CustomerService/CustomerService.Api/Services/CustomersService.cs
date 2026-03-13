using Microsoft.EntityFrameworkCore;
using CustomerService.Api.Data;
using CustomerService.Api.Models;

namespace CustomerService.Api.Services;

public class CustomersService
{
    private readonly CustomerDbContext _context;

    public CustomersService(CustomerDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        return await _context.Customers.ToListAsync();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer?> UpdateCustomerAsync(int id, Customer customer)
    {
        var existing = await _context.Customers.FindAsync(id);
        if (existing is null) return null;

        existing.Name = customer.Name;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.State = customer.State;
        existing.ZipCode = customer.ZipCode;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer is null) return false;

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        return true;
    }
}
