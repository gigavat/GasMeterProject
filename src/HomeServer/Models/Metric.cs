namespace MetricsApi.Models;

public class Metric
{
    public int Id { get; set; }
    public long PulseCount { get; set; }
    public string Timings { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

