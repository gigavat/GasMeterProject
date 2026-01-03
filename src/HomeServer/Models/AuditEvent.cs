namespace MetricsApi.Models;

public class AuditEvent
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty; // Connected, HealthCheck, ApiRequest, Error, etc.
    public string Description { get; set; } = string.Empty;
    public string? Source { get; set; } // IP address, service name, etc.
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public int? StatusCode { get; set; }
    public string? AdditionalData { get; set; } // JSON string for extra context
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

