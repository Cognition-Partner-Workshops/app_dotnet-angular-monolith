using OrderManager.Api.Models;

namespace OrderManager.Api.Data;

/// <summary>
/// Provides seed data initialization for the OrderManager database.
/// Populates the database with sample customers, products, and inventory items
/// for local development and demonstration purposes.
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Seeds the database with initial sample data if it has not already been populated.
    /// Creates the database schema if it does not exist, then inserts sample customers,
    /// products, and inventory items. This method is idempotent — it skips seeding if
    /// products already exist in the database.
    /// </summary>
    /// <param name="context">The application database context used to insert seed data.</param>
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        // Skip seeding if data already exists to ensure idempotency
        if (context.Products.Any()) return;

        // Seed sample customer records
        var customers = new[]
        {
            new Customer { Name = "Acme Corp", Email = "orders@acme.com", Phone = "555-0100", Address = "123 Main St", City = "Springfield", State = "IL", ZipCode = "62701" },
            new Customer { Name = "Globex Inc", Email = "purchasing@globex.com", Phone = "555-0200", Address = "456 Oak Ave", City = "Shelbyville", State = "IL", ZipCode = "62565" },
            new Customer { Name = "Initech LLC", Email = "supplies@initech.com", Phone = "555-0300", Address = "789 Pine Rd", City = "Capital City", State = "IL", ZipCode = "62702" },
        };
        context.Customers.AddRange(customers);

        // Seed sample product catalog
        var products = new[]
        {
            new Product { Name = "Widget A", Description = "Standard widget", Category = "Widgets", Price = 9.99m, Sku = "WGT-001" },
            new Product { Name = "Widget B", Description = "Premium widget", Category = "Widgets", Price = 19.99m, Sku = "WGT-002" },
            new Product { Name = "Gadget X", Description = "Basic gadget", Category = "Gadgets", Price = 29.99m, Sku = "GDG-001" },
            new Product { Name = "Gadget Y", Description = "Advanced gadget", Category = "Gadgets", Price = 49.99m, Sku = "GDG-002" },
            new Product { Name = "Thingamajig", Description = "Multi-purpose thingamajig", Category = "Misc", Price = 14.99m, Sku = "THG-001" },
        };
        context.Products.AddRange(products);
        context.SaveChanges();

        // Seed inventory items with incrementing stock quantities and sequential warehouse locations
        var inventoryItems = products.Select((p, i) => new InventoryItem
        {
            ProductId = p.Id,
            QuantityOnHand = (i + 1) * 50,
            ReorderLevel = 10,
            WarehouseLocation = $"A-{i + 1:D2}"
        }).ToArray();
        context.InventoryItems.AddRange(inventoryItems);
        context.SaveChanges();
    }
}
