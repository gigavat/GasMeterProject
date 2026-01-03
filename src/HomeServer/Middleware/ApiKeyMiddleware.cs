using MetricsApi.Services;

namespace MetricsApi.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private const string API_KEY_HEADER = "X-API-Key";

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration, IAuditService auditService)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var requestPath = context.Request.Path.Value ?? "";
        var requestMethod = context.Request.Method;

        // Skip API key check for Swagger and health check endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.StartsWith("/swagger") || path.StartsWith("/api/health"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key was not provided.");
            
            // Log authentication failure
            await auditService.LogEventAsync(
                eventType: "AuthenticationFailure",
                description: "API Key was not provided",
                source: clientIp,
                userAgent: userAgent,
                requestPath: requestPath,
                requestMethod: requestMethod,
                statusCode: 401
            );
            
            return;
        }

        var apiKey = configuration["ApiKey"] ?? throw new InvalidOperationException("ApiKey is not configured");
        var extractedApiKeyString = extractedApiKey.ToString();

        if (!apiKey.Equals(extractedApiKeyString))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid API Key.");
            
            // Log authentication failure with masked key
            var maskedKey = extractedApiKeyString.Length > 4 
                ? extractedApiKeyString.Substring(0, 4) + "***" 
                : "***";
            
            await auditService.LogEventAsync(
                eventType: "AuthenticationFailure",
                description: $"Invalid API Key provided: {maskedKey}",
                source: clientIp,
                userAgent: userAgent,
                requestPath: requestPath,
                requestMethod: requestMethod,
                statusCode: 401,
                additionalData: System.Text.Json.JsonSerializer.Serialize(new { KeyLength = extractedApiKeyString.Length })
            );
            
            return;
        }

        // Log successful authentication
        await auditService.LogEventAsync(
            eventType: "AuthenticationSuccess",
            description: "API Key validated successfully",
            source: clientIp,
            userAgent: userAgent,
            requestPath: requestPath,
            requestMethod: requestMethod,
            statusCode: 200
        );

        await _next(context);
    }
}

