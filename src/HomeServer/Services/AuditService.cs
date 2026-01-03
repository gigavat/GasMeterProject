using MetricsApi.Data;
using MetricsApi.Models;

namespace MetricsApi.Services;

public interface IAuditService
{
    Task LogEventAsync(string eventType, string description, string? source = null, 
        string? userAgent = null, string? requestPath = null, string? requestMethod = null, 
        int? statusCode = null, string? additionalData = null);
}

public class AuditService : IAuditService
{
    private readonly MetricsDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(MetricsDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogEventAsync(string eventType, string description, string? source = null, 
        string? userAgent = null, string? requestPath = null, string? requestMethod = null, 
        int? statusCode = null, string? additionalData = null)
    {
        try
        {
            var auditEvent = new AuditEvent
            {
                EventType = eventType,
                Description = description,
                Source = source,
                UserAgent = userAgent,
                RequestPath = requestPath,
                RequestMethod = requestMethod,
                StatusCode = statusCode,
                AdditionalData = additionalData,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditEvents.Add(auditEvent);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Audit event logged: {EventType} - {Description}", eventType, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event: {EventType}", eventType);
            // Don't throw - audit logging should not break the application
        }
    }
}

