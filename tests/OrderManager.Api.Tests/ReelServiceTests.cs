using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.DTOs;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

public class ReelServiceTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        SeedData.Initialize(context);
        return context;
    }

    [Fact]
    public async Task GetFeed_ReturnsPaginatedReels()
    {
        using var context = CreateContext();
        var service = new ReelService(context);

        var feed = await service.GetFeedAsync(1, 3, null);

        Assert.NotNull(feed);
        Assert.Equal(3, feed.Reels.Count);
        Assert.Equal(5, feed.TotalCount);
        Assert.True(feed.HasMore);
    }

    [Fact]
    public async Task GetFeed_ReturnsLastPage()
    {
        using var context = CreateContext();
        var service = new ReelService(context);

        var feed = await service.GetFeedAsync(2, 3, null);

        Assert.Equal(2, feed.Reels.Count);
        Assert.False(feed.HasMore);
    }

    [Fact]
    public async Task GetById_IncrementsViewCount()
    {
        using var context = CreateContext();
        var service = new ReelService(context);
        var firstReel = await context.Reels.FirstAsync();
        var initialViews = firstReel.ViewCount;

        var reel = await service.GetByIdAsync(firstReel.Id, null);

        Assert.NotNull(reel);
        Assert.Equal(initialViews + 1, reel!.ViewCount);
    }

    [Fact]
    public async Task ToggleLike_AddsAndRemovesLike()
    {
        using var context = CreateContext();
        var service = new ReelService(context);
        var reel = await context.Reels.FirstAsync();
        var user = await context.AppUsers.FirstAsync();
        var initialLikes = reel.LikeCount;

        // Like
        var liked = await service.ToggleLikeAsync(reel.Id, user.Id);
        Assert.True(liked);
        await context.Entry(reel).ReloadAsync();
        Assert.Equal(initialLikes + 1, reel.LikeCount);

        // Unlike
        var unliked = await service.ToggleLikeAsync(reel.Id, user.Id);
        Assert.False(unliked);
        await context.Entry(reel).ReloadAsync();
        Assert.Equal(initialLikes, reel.LikeCount);
    }

    [Fact]
    public async Task Create_AddsNewReel()
    {
        using var context = CreateContext();
        var service = new ReelService(context);
        var user = await context.AppUsers.FirstAsync();

        var request = new CreateReelRequest
        {
            Title = "Test Reel",
            Description = "A test reel",
            Tags = "test,demo"
        };

        var reel = await service.CreateAsync(request, user.Id, "/uploads/test.mp4", null, 30, 1024);

        Assert.NotNull(reel);
        Assert.Equal("Test Reel", reel.Title);
        Assert.Equal(user.Id, reel.Creator.Id);
    }

    [Fact]
    public async Task GetUserReels_ReturnsOnlyUserReels()
    {
        using var context = CreateContext();
        var service = new ReelService(context);
        var user = await context.AppUsers.FirstAsync();

        var reels = await service.GetUserReelsAsync(user.Id);
        Assert.All(reels, r => Assert.Equal(user.Id, r.Creator.Id));
    }

    [Fact]
    public async Task ToggleLike_ThrowsForNonExistentReel()
    {
        using var context = CreateContext();
        var service = new ReelService(context);
        var user = await context.AppUsers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ToggleLikeAsync(99999, user.Id));
    }
}
