using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManager.Api.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrderManager.Api.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("IntegrationTestDb"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Auth_Register_And_Login_Flow()
    {
        var uniqueUser = "integtest_" + Guid.NewGuid().ToString("N")[..8];

        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = uniqueUser,
            email = $"{uniqueUser}@test.com",
            password = "Test@1234",
            displayName = "Integration Test User"
        });
        Assert.Equal(HttpStatusCode.OK, regResponse.StatusCode);

        var regContent = await regResponse.Content.ReadAsStringAsync();
        var regDoc = JsonDocument.Parse(regContent);
        Assert.True(regDoc.RootElement.TryGetProperty("accessToken", out _));
        Assert.True(regDoc.RootElement.TryGetProperty("refreshToken", out _));

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = uniqueUser,
            password = "Test@1234"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginContent);
        var token = loginDoc.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrEmpty(token));

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var meResponse = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var meContent = await meResponse.Content.ReadAsStringAsync();
        var meDoc = JsonDocument.Parse(meContent);
        Assert.Equal(uniqueUser, meDoc.RootElement.GetProperty("username").GetString());

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Auth_Login_Returns401ForInvalidCredentials()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "nonexistent_user_xyz",
            password = "WrongPassword1!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Auth_Me_Returns401WithoutToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Auth_Register_ReturnsConflictForDuplicateUser()
    {
        var uniqueUser = "duptest_" + Guid.NewGuid().ToString("N")[..8];

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = uniqueUser,
            email = $"{uniqueUser}@test.com",
            password = "Test@1234",
            displayName = "First User"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = uniqueUser,
            email = $"{uniqueUser}2@test.com",
            password = "Test@1234",
            displayName = "Second User"
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Auth_RefreshToken_Works()
    {
        var uniqueUser = "refresh_" + Guid.NewGuid().ToString("N")[..8];

        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = uniqueUser,
            email = $"{uniqueUser}@test.com",
            password = "Test@1234",
            displayName = "Refresh Test User"
        });
        var regContent = await regResponse.Content.ReadAsStringAsync();
        var regDoc = JsonDocument.Parse(regContent);
        var refreshToken = regDoc.RootElement.GetProperty("refreshToken").GetString();

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken
        });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Reels_FeedIsPubliclyAccessible()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var feedResponse = await _client.GetAsync("/api/reels");
        Assert.Equal(HttpStatusCode.OK, feedResponse.StatusCode);
    }

    [Fact]
    public async Task Reels_CreateRequiresAuthentication()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/reels/my");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reels_GetFeed_ReturnsReelsWhenAuthenticated()
    {
        var token = await RegisterAndGetToken("reelfeed");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/reels?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Calls_Endpoints_RequireAuthentication()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var contactsResponse = await _client.GetAsync("/api/calls/contacts");
        Assert.Equal(HttpStatusCode.Unauthorized, contactsResponse.StatusCode);

        var historyResponse = await _client.GetAsync("/api/calls/history");
        Assert.Equal(HttpStatusCode.Unauthorized, historyResponse.StatusCode);
    }

    [Fact]
    public async Task Calls_GetContacts_ReturnsListWhenAuthenticated()
    {
        var token = await RegisterAndGetToken("callcontact");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/calls/contacts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Calls_GetHistory_ReturnsListWhenAuthenticated()
    {
        var token = await RegisterAndGetToken("callhist");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/calls/history");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Security_SwaggerEndpointIsAccessible()
    {
        var response = await _client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Security_InvalidEndpointReturns404OrFallback()
    {
        var response = await _client.GetAsync("/api/nonexistent");
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private async Task<string> RegisterAndGetToken(string prefix)
    {
        var uniqueUser = $"{prefix}_{Guid.NewGuid():N}"[..20];
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = uniqueUser,
            email = $"{uniqueUser}@test.com",
            password = "Test@1234",
            displayName = "Test User"
        });
        regResponse.EnsureSuccessStatusCode();
        var content = await regResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }
}
