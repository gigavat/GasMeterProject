using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetricsApi.Data;
using MetricsApi.Models;
using MetricsApi.Services;
using MetricsApi.Dtos;
using System.Text.Json;

namespace MetricsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly MetricsDbContext _context;
    private readonly ILogger<MetricsController> _logger;
    private readonly IAuditService _auditService;

    public MetricsController(
        MetricsDbContext context, 
        ILogger<MetricsController> logger,
        IAuditService auditService)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMetric([FromBody] MetricDto dto)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();

        if (!ModelState.IsValid)
        {
            await _auditService.LogEventAsync(
                eventType: "ApiRequest",
                description: "Failed to create metrics - validation error",
                source: clientIp,
                userAgent: userAgent,
                requestPath: "/api/metrics",
                requestMethod: "POST",
                statusCode: 400
            );
            return BadRequest(ModelState);
        }

        try
        {
            var metric = new Metric
            {
                PulseCount = dto.PulseCount,
                Timings = JsonSerializer.Serialize(dto.Timings, JsonSerializerOptions.Default),
                CreatedAt = DateTime.UtcNow,
            };

            _context.Metrics.Add(metric);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created metric {MetricId} at {CreatedAt}", metric.Id, metric.CreatedAt);

            // Log successful metrics creation
            await _auditService.LogEventAsync(
                eventType: "MetricCreated",
                description: $"Created metric {metric.Id}",
                source: clientIp,
                userAgent: userAgent,
                requestPath: "/api/metrics",
                requestMethod: "POST",
                statusCode: 201,
                additionalData: System.Text.Json.JsonSerializer.Serialize(new 
                { 
                    MetricId = metric.Id
                })
            );

            return StatusCode(201, metric);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating metrics");
            
            await _auditService.LogEventAsync(
                eventType: "Error",
                description: $"Error creating metrics: {ex.Message}",
                source: clientIp,
                userAgent: userAgent,
                requestPath: "/api/metrics",
                requestMethod: "POST",
                statusCode: 500,
                additionalData: ex.ToString()
            );
            
            return StatusCode(500, "An error occurred while creating the metrics");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMetric(int id)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();

        var metric = await _context.Metrics.FindAsync(id);

        if (metric == null)
        {
            await _auditService.LogEventAsync(
                eventType: "ApiRequest",
                description: $"Metric not found: ID {id}",
                source: clientIp,
                userAgent: userAgent,
                requestPath: $"/api/metrics/{id}",
                requestMethod: "GET",
                statusCode: 404
            );
            return NotFound();
        }

        await _auditService.LogEventAsync(
            eventType: "ApiRequest",
            description: $"Metric retrieved: ID {id}",
            source: clientIp,
            userAgent: userAgent,
            requestPath: $"/api/metrics/{id}",
            requestMethod: "GET",
            statusCode: 200
        );

        return Ok(metric);
    }

    [HttpGet]
    public async Task<IActionResult> GetMetrics([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();

        var metrics = await _context.Metrics
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        await _auditService.LogEventAsync(
            eventType: "ApiRequest",
            description: $"Metrics list retrieved: page {page}, pageSize {pageSize}, count {metrics.Count}",
            source: clientIp,
            userAgent: userAgent,
            requestPath: "/api/metrics",
            requestMethod: "GET",
            statusCode: 200,
            additionalData: System.Text.Json.JsonSerializer.Serialize(new { Page = page, PageSize = pageSize, Count = metrics.Count })
        );

        return Ok(metrics);
    }
}

