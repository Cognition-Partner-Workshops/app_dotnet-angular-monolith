using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderManager.Api.Data;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

public class AuthServiceTests
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

    private IConfiguration CreateConfig()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json", optional: true)
            .Build();
        // Provide defaults if file doesn't exist
        if (string.IsNullOrEmpty(config["Jwt:Key"]))
        {
            var dict = new Dictionary<string, string?>
            {
                {"Jwt:Key", Convert.ToBase64String(new byte[32])},
                {"Jwt:Issuer", "TrainConnect"},
                {"Jwt:Audience", "TrainConnectApp"}
            };
            config = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();
        }
        return config;
    }

    [Fact]
    public async Task Register_CreatesNewUser()
    {
        using var context = CreateContext();
        var service = new AuthService(context, CreateConfig());

        var result = await service.RegisterAsync(new DTOs.RegisterRequest
        {
            Username = "testuser",
            Email = "test@test.com",
            Password = "Test@1234",
            DisplayName = "Test User"
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.Equal("testuser", result.User.Username);
    }

    [Fact]
    public async Task Register_ThrowsOnDuplicateUsername()
    {
        using var context = CreateContext();
        var service = new AuthService(context, CreateConfig());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(new DTOs.RegisterRequest
            {
                Username = "alice",
                Email = "newalice@test.com",
                Password = "Test@1234",
                DisplayName = "Alice 2"
            }));
    }

    [Fact]
    public async Task Login_ReturnsTokenForValidCredentials()
    {
        using var context = CreateContext();
        var service = new AuthService(context, CreateConfig());

        var result = await service.LoginAsync(new DTOs.LoginRequest
        {
            Username = "alice",
            Password = "Demo@123"
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.Equal("alice", result.User.Username);
    }

    [Fact]
    public async Task Login_ThrowsForInvalidPassword()
    {
        using var context = CreateContext();
        var service = new AuthService(context, CreateConfig());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new DTOs.LoginRequest
            {
                Username = "alice",
                Password = "WrongPassword1!"
            }));
    }

    [Fact]
    public async Task RefreshToken_WorksWithValidToken()
    {
        using var context = CreateContext();
        var service = new AuthService(context, CreateConfig());

        var loginResult = await service.LoginAsync(new DTOs.LoginRequest
        {
            Username = "alice",
            Password = "Demo@123"
        });

        var refreshResult = await service.RefreshTokenAsync(loginResult.RefreshToken);
        Assert.NotNull(refreshResult);
        Assert.NotEmpty(refreshResult.AccessToken);
    }

    [Fact]
    public async Task RefreshToken_ThrowsForInvalidToken()
    {
        using var context = CreateContext();
        var service = new AuthService(context, CreateConfig());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RefreshTokenAsync("invalid-token"));
    }

    [Fact]
    public async Task Logout_SetsUserOffline()
    {
        using var context = CreateContext();
        var service = new AuthService(context, CreateConfig());

        await service.LoginAsync(new DTOs.LoginRequest
        {
            Username = "alice",
            Password = "Demo@123"
        });

        var user = await context.AppUsers.FirstAsync(u => u.Username == "alice");
        Assert.True(user.IsOnline);

        await service.LogoutAsync(user.Id);
        await context.Entry(user).ReloadAsync();
        Assert.False(user.IsOnline);
    }

    [Fact]
    public void PasswordHashing_VerifiesCorrectly()
    {
        var password = "SecureP@ss123";
        var hash = SeedData.HashPassword(password);
        Assert.True(SeedData.VerifyPassword(password, hash));
        Assert.False(SeedData.VerifyPassword("WrongPassword", hash));
    }
}
