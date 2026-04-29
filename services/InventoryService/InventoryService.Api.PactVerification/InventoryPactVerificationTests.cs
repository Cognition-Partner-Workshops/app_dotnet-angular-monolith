using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InventoryService.Api.Data;
using PactNet;
using PactNet.Verifier;
using Xunit;

namespace InventoryService.Api.PactVerification;

public class InventoryPactVerificationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public InventoryPactVerificationTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<InventoryDbContext>(options =>
                        options.UseInMemoryDatabase("PactVerification_" + Guid.NewGuid()));
                });
            });
    }

    [Fact(Skip = "Run after consumer pact files are generated")]
    public void VerifyInventoryServiceHonoursOrderServicePact()
    {
        var pactPath = Path.Combine("..", "..", "..", "..", "OrderService", "OrderService.Api.ContractTests", "pacts");

        if (!Directory.Exists(pactPath))
            return;

        var pactFile = Path.Combine(pactPath, "OrderService-InventoryService.json");
        if (!File.Exists(pactFile))
            return;

        using var server = _factory.Server;
        var verifier = new PactVerifier("InventoryService", new PactVerifierConfig());

        verifier
            .WithHttpEndpoint(server.BaseAddress)
            .WithFileSource(new FileInfo(pactFile))
            .Verify();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
