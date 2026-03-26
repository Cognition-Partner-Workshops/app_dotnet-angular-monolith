using OrderManager.Api.Models;
using System.Security.Cryptography;

namespace OrderManager.Api.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Products.Any()) return;

        var customers = new[]
        {
            new Customer { Name = "Acme Corp", Email = "orders@acme.com", Phone = "555-0100", Address = "123 Main St", City = "Springfield", State = "IL", ZipCode = "62701" },
            new Customer { Name = "Globex Inc", Email = "purchasing@globex.com", Phone = "555-0200", Address = "456 Oak Ave", City = "Shelbyville", State = "IL", ZipCode = "62565" },
            new Customer { Name = "Initech LLC", Email = "supplies@initech.com", Phone = "555-0300", Address = "789 Pine Rd", City = "Capital City", State = "IL", ZipCode = "62702" },
        };
        context.Customers.AddRange(customers);

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

        var inventoryItems = products.Select((p, i) => new InventoryItem
        {
            ProductId = p.Id,
            QuantityOnHand = (i + 1) * 50,
            ReorderLevel = 10,
            WarehouseLocation = $"A-{i + 1:D2}"
        }).ToArray();
        context.InventoryItems.AddRange(inventoryItems);
        context.SaveChanges();

        SeedTrainConnectData(context);
    }

    private static void SeedTrainConnectData(AppDbContext context)
    {
        if (context.AppUsers.Any()) return;

        var demoPasswordHash = HashPassword("Demo@123");

        var users = new[]
        {
            new User { Username = "alice", Email = "alice@trainconnect.app", PasswordHash = demoPasswordHash, DisplayName = "Alice Sharma", IsOnline = false },
            new User { Username = "bob", Email = "bob@trainconnect.app", PasswordHash = demoPasswordHash, DisplayName = "Bob Kumar", IsOnline = false },
            new User { Username = "charlie", Email = "charlie@trainconnect.app", PasswordHash = demoPasswordHash, DisplayName = "Charlie Patel", IsOnline = false },
        };
        context.AppUsers.AddRange(users);
        context.SaveChanges();

        var reels = new[]
        {
            new Reel { Title = "Beautiful Sunset from Train Window", Description = "Captured this amazing sunset during my Mumbai-Goa journey", VideoUrl = "/assets/reels/sunset.mp4", ThumbnailUrl = "/assets/reels/thumbs/sunset.jpg", DurationSeconds = 30, ViewCount = 1250, LikeCount = 340, UserId = users[0].Id, Tags = "sunset,train,travel", IsDownloadable = true, FileSizeBytes = 5242880 },
            new Reel { Title = "Mountain Pass Through the Clouds", Description = "Incredible mountain views on the Shimla Express", VideoUrl = "/assets/reels/mountains.mp4", ThumbnailUrl = "/assets/reels/thumbs/mountains.jpg", DurationSeconds = 45, ViewCount = 2100, LikeCount = 580, UserId = users[1].Id, Tags = "mountains,scenic,railway", IsDownloadable = true, FileSizeBytes = 7340032 },
            new Reel { Title = "Chai Vendor on Platform", Description = "The iconic chai walla at Nagpur Junction", VideoUrl = "/assets/reels/chai.mp4", ThumbnailUrl = "/assets/reels/thumbs/chai.jpg", DurationSeconds = 15, ViewCount = 4500, LikeCount = 1200, UserId = users[2].Id, Tags = "chai,culture,station", IsDownloadable = true, FileSizeBytes = 2621440 },
            new Reel { Title = "Train Crossing Bridge at Dawn", Description = "Crossing the Pamban Bridge - a breathtaking experience", VideoUrl = "/assets/reels/bridge.mp4", ThumbnailUrl = "/assets/reels/thumbs/bridge.jpg", DurationSeconds = 60, ViewCount = 8700, LikeCount = 3200, UserId = users[0].Id, Tags = "bridge,dawn,iconic", IsDownloadable = true, FileSizeBytes = 10485760 },
            new Reel { Title = "Tunnel Experience on Konkan Railway", Description = "Going through 92 tunnels on the Konkan route!", VideoUrl = "/assets/reels/tunnel.mp4", ThumbnailUrl = "/assets/reels/thumbs/tunnel.jpg", DurationSeconds = 20, ViewCount = 3300, LikeCount = 890, UserId = users[1].Id, Tags = "tunnel,konkan,adventure", IsDownloadable = true, FileSizeBytes = 3145728 },
        };
        context.Reels.AddRange(reels);
        context.SaveChanges();

        var contacts = new[]
        {
            new Contact { UserId = users[0].Id, ContactUserId = users[1].Id, DisplayName = "Bob Kumar" },
            new Contact { UserId = users[0].Id, ContactUserId = users[2].Id, DisplayName = "Charlie Patel" },
            new Contact { UserId = users[1].Id, ContactUserId = users[0].Id, DisplayName = "Alice Sharma" },
            new Contact { UserId = users[2].Id, ContactUserId = users[0].Id, DisplayName = "Alice Sharma" },
        };
        context.Contacts.AddRange(contacts);
        context.SaveChanges();
    }

    public static string HashPassword(string password)
    {
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);
        byte[] hashBytes = new byte[48];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);
        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        byte[] hashBytes = Convert.FromBase64String(storedHash);
        byte[] salt = new byte[16];
        Array.Copy(hashBytes, 0, salt, 0, 16);
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);
        for (int i = 0; i < 32; i++)
        {
            if (hashBytes[i + 16] != hash[i]) return false;
        }
        return true;
    }
}
