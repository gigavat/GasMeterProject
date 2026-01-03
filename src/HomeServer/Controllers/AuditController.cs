using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetricsApi.Data;

namespace MetricsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly MetricsDbContext _context;
    private readonly ILogger<AuditController> _logger;

    public AuditController(MetricsDbContext context, ILogger<AuditController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditEvents(
        [FromQuery] string? eventType = null,
        [FromQuery] string? source = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var query = _context.AuditEvents.AsQueryable();

            if (!string.IsNullOrEmpty(eventType))
            {
                query = query.Where(e => e.EventType == eventType);
            }

            if (!string.IsNullOrEmpty(source))
            {
                query = query.Where(e => e.Source == source);
            }

            if (startDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt <= endDate.Value);
            }

            var totalCount = await query.CountAsync();
            var events = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Events = events
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit events");
            return StatusCode(500, "An error occurred while retrieving audit events");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAuditEvent(int id)
    {
        var auditEvent = await _context.AuditEvents.FindAsync(id);

        if (auditEvent == null)
        {
            return NotFound();
        }

        return Ok(auditEvent);
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetEventTypes()
    {
        var eventTypes = await _context.AuditEvents
            .Select(e => e.EventType)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        return Ok(eventTypes);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetAuditStats(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var query = _context.AuditEvents.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt <= endDate.Value);
            }

            var stats = await query
                .GroupBy(e => e.EventType)
                .Select(g => new
                {
                    EventType = g.Key,
                    Count = g.Count(),
                    LastOccurrence = g.Max(e => e.CreatedAt)
                })
                .OrderByDescending(s => s.Count)
                .ToListAsync();

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit stats");
            return StatusCode(500, "An error occurred while retrieving audit stats");
        }
    }
}

