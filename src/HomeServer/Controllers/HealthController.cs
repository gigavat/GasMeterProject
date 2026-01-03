using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetricsApi.Data;
using MetricsApi.Services;

namespace MetricsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly MetricsDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        MetricsDbContext context, 
        IAuditService auditService,
        ILogger<HealthController> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> HealthCheck()
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();

        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (canConnect)
            {
                // Log health check to audit
                await _auditService.LogEventAsync(
                    eventType: "HealthCheck",
                    description: "Health check performed successfully",
                    source: clientIp,
                    userAgent: userAgent,
                    requestPath: "/api/health",
                    requestMethod: "GET",
                    statusCode: 200
                );

                return Ok(new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Database = "Connected"
                });
            }
            else
            {
                await _auditService.LogEventAsync(
                    eventType: "HealthCheck",
                    description: "Health check failed - database not connected",
                    source: clientIp,
                    userAgent: userAgent,
                    requestPath: "/api/health",
                    requestMethod: "GET",
                    statusCode: 503
                );

                return StatusCode(503, new
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Database = "Disconnected"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check error");
            
            await _auditService.LogEventAsync(
                eventType: "HealthCheck",
                description: $"Health check error: {ex.Message}",
                source: clientIp,
                userAgent: userAgent,
                requestPath: "/api/health",
                requestMethod: "GET",
                statusCode: 500,
                additionalData: ex.ToString()
            );

            return StatusCode(500, new
            {
                Status = "Error",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            });
        }
    }
}

