using Microsoft.AspNetCore.Http;

namespace CoworkingAPI.Middleware;

public class SignalRQueryStringAuthMiddleware
{
    private readonly RequestDelegate _next;

    public SignalRQueryStringAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var path = request.Path;

        if (path.StartsWithSegments("/hubs/alert-telemetry") && request.Query.TryGetValue("access_token", out var accessToken))
        {
            if (!request.Headers.ContainsKey("Authorization"))
            {
                request.Headers.Append("Authorization", $"Bearer {accessToken}");
            }
        }

        await _next(context);
    }
}
