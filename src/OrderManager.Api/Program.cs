using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderManager.Api.Data;
using OrderManager.Api.Hubs;
using OrderManager.Api.Middleware;
using OrderManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ordermanager.db"));

// Original OrderManager services
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<InventoryService>();

// TrainConnect services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ReelService>();
builder.Services.AddScoped<CallService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    // Generate a random key for development; set Jwt__Key env var in production
    jwtKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    builder.Configuration["Jwt:Key"] = jwtKey;
}
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TrainConnect",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TrainConnectApp",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Allow JWT from X-Authorization header (tunnel proxy overrides Authorization)
    // and from query string for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Check X-Authorization header first (used when behind proxy with Basic Auth)
            var xAuth = context.Request.Headers["X-Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xAuth) && xAuth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = xAuth.Substring("Bearer ".Length).Trim();
                return Task.CompletedTask;
            }

            // Allow SignalR to receive JWT from query string
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            // Suppress the WWW-Authenticate: Bearer header on 401 responses
            // to prevent browsers from showing a native Basic Auth popup
            // when behind a reverse proxy that also uses Basic Auth
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"error\":\"Unauthorized\"}");
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.Initialize(context);
}

// Security middleware
app.UseSecurityHeaders();
app.UseRateLimiting(maxRequests: 100, windowSeconds: 60);

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
// Angular 17 builds to wwwroot/browser/
var browserPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "browser");
if (Directory.Exists(browserPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(browserPath)
    });
}
else
{
    app.UseStaticFiles();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CallHub>("/hubs/call");
app.MapFallback(async context =>
{
    var bPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "browser", "index.html");
    if (File.Exists(bPath))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(bPath);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});
app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
