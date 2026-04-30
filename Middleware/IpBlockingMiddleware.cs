using System.Net;
using Services;

namespace UniWebApp.Middleware;

public class IpBlockingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpBlockingMiddleware> _logger;
    private readonly IIpBlockingService _ipBlockingService;

    public IpBlockingMiddleware(
        RequestDelegate next,
        ILogger<IpBlockingMiddleware> logger,
        IIpBlockingService ipBlockingService)
    {
        _next = next;
        _logger = logger;
        _ipBlockingService = ipBlockingService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);

        if (string.IsNullOrEmpty(clientIp))
        {
            _logger.LogDebug("Could not determine client IP address for request to {Path}", context.Request.Path);
            await _next(context);
            return;
        }

        if (context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().IsDevelopment())
        {
            _logger.LogInformation("Request from IP: {IpAddress} to {Path}", clientIp, context.Request.Path);
        }

        if (_ipBlockingService.IsBlocked(clientIp))
        {
            _logger.LogWarning("Blocked request from IP: {IpAddress} to {Path}", clientIp, context.Request.Path);
            
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Access denied. Your IP address has been blocked.");
            return;
        }

        await _next(context);
    }

    private string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (ips.Length > 0 && IsValidIpAddress(ips[0]))
            {
                return ips[0];
            }
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp) && IsValidIpAddress(realIp))
        {
            return realIp;
        }

        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            if (remoteIp.IsIPv4MappedToIPv6)
            {
                return remoteIp.MapToIPv4().ToString();
            }
            
            if (remoteIp.Equals(IPAddress.IPv6Loopback))
            {
                return "127.0.0.1";
            }
            
            return remoteIp.ToString();
        }

        return null;
    }

    private bool IsValidIpAddress(string ip)
    {
        return IPAddress.TryParse(ip, out _);
    }
}