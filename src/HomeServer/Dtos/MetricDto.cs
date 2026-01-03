namespace MetricsApi.Dtos;

public class MetricDto
{
    public long PulseCount { get; set; }
    public List<long> Timings { get; set; } = null!;
}

