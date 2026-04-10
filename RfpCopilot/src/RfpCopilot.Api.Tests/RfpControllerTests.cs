using Microsoft.EntityFrameworkCore;
using RfpCopilot.Api.Data;
using RfpCopilot.Api.Models;
using Xunit;

namespace RfpCopilot.Api.Tests;

public class RfpControllerTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SeedData_ShouldCreate10Entries()
    {
        using var context = CreateInMemoryContext();
        SeedData.Initialize(context);

        var entries = await context.RfpTrackerEntries.ToListAsync();
        Assert.Equal(10, entries.Count);
    }

    [Fact]
    public async Task SeedData_ShouldHaveCorrectStatusForMissingCrm()
    {
        using var context = CreateInMemoryContext();
        SeedData.Initialize(context);

        var pendingEntries = await context.RfpTrackerEntries
            .Where(e => string.IsNullOrEmpty(e.CrmId))
            .ToListAsync();

        Assert.All(pendingEntries, e => Assert.Equal("Pending CRM", e.Status));
    }

    [Fact]
    public async Task SeedData_ShouldNotDuplicateOnRerun()
    {
        using var context = CreateInMemoryContext();
        SeedData.Initialize(context);
        SeedData.Initialize(context);

        var entries = await context.RfpTrackerEntries.ToListAsync();
        Assert.Equal(10, entries.Count);
    }
}
