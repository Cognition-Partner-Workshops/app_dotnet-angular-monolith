using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

// DECOMPOSITION NOTE: The Customers domain has been extracted to a dedicated microservice.
// See: https://github.com/Cognition-Partner-Workshops/app_dotnet-angular-microservices
// The Customer microservice is now the source of truth for customer data.
// This monolith service is retained because the Orders module still depends on Customer data
// via the shared AppDbContext. Once Orders is also extracted, this service can be removed.
public class CustomerService
{
    private readonly AppDbContext _context;

    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        return await _context.Customers.ToListAsync();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _context.Customers.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }
}
