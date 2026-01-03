using MetricsApi.Services;

namespace MetricsApi.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        var startTime = DateTime.UtcNow;
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var requestPath = context.Request.Path.Value ?? "";
        var requestMethod = context.Request.Method;

        // Skip audit logging for health checks and swagger endpoints (they have their own logging)
        if (requestPath.StartsWith("/api/health", StringComparison.OrdinalIgnoreCase) ||
            requestPath.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        try
        {
            await _next(context);

            var statusCode = context.Response.StatusCode;
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Log API request
            await auditService.LogEventAsync(
                eventType: "ApiRequest",
                description: $"{requestMethod} {requestPath} - Status: {statusCode} - Duration: {duration:F2}ms",
                source: clientIp,
                userAgent: userAgent,
                requestPath: requestPath,
                requestMethod: requestMethod,
                statusCode: statusCode,
                additionalData: System.Text.Json.JsonSerializer.Serialize(new { DurationMs = duration })
            );
        }
        catch (Exception ex)
        {
            var statusCode = context.Response.StatusCode > 0 ? context.Response.StatusCode : 500;
            
            await auditService.LogEventAsync(
                eventType: "Error",
                description: $"{requestMethod} {requestPath} - Exception: {ex.Message}",
                source: clientIp,
                userAgent: userAgent,
                requestPath: requestPath,
                requestMethod: requestMethod,
                statusCode: statusCode,
                additionalData: ex.ToString()
            );

            throw;
        }
    }
}

