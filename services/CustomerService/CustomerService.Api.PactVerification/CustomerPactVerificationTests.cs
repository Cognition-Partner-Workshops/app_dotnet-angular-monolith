using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CustomerService.Api.Data;
using PactNet;
using PactNet.Verifier;
using Xunit;

namespace CustomerService.Api.PactVerification;

public class CustomerPactVerificationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public CustomerPactVerificationTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CustomerDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<CustomerDbContext>(options =>
                        options.UseInMemoryDatabase("PactVerification_" + Guid.NewGuid()));
                });
            });
    }

    [Fact(Skip = "Run after consumer pact files are generated")]
    public void VerifyCustomerServiceHonoursOrderServicePact()
    {
        var pactPath = Path.Combine("..", "..", "..", "..", "OrderService", "OrderService.Api.ContractTests", "pacts");

        if (!Directory.Exists(pactPath))
            return;

        var pactFile = Path.Combine(pactPath, "OrderService-CustomerService.json");
        if (!File.Exists(pactFile))
            return;

        using var server = _factory.Server;
        var verifier = new PactVerifier("CustomerService", new PactVerifierConfig());

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
