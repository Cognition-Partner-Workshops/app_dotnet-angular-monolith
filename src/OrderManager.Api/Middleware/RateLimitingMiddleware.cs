using System.Collections.Concurrent;

namespace OrderManager.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, ClientRateInfo> Clients = new();
    private readonly int _maxRequests;
    private readonly TimeSpan _window;

    public RateLimitingMiddleware(RequestDelegate next, int maxRequests = 100, int windowSeconds = 60)
    {
        _next = next;
        _maxRequests = maxRequests;
        _window = TimeSpan.FromSeconds(windowSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var clientKey = $"{clientIp}_{context.Request.Path}";

        var clientInfo = Clients.GetOrAdd(clientKey, _ => new ClientRateInfo());

        lock (clientInfo)
        {
            var now = DateTime.UtcNow;
            if (now - clientInfo.WindowStart > _window)
            {
                clientInfo.WindowStart = now;
                clientInfo.RequestCount = 0;
            }

            clientInfo.RequestCount++;

            if (clientInfo.RequestCount > _maxRequests)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = ((int)(_window - (now - clientInfo.WindowStart)).TotalSeconds).ToString();
                context.Response.ContentType = "application/json";
                context.Response.WriteAsync("{\"error\":\"Too many requests. Please try again later.\"}");
                return;
            }
        }

        await _next(context);
    }

    private class ClientRateInfo
    {
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        public int RequestCount { get; set; }
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, int maxRequests = 100, int windowSeconds = 60)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>(maxRequests, windowSeconds);
    }
}
