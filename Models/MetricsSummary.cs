using System.Collections.Generic;

namespace SqlStressRunner.Models;

public class MetricsSummary
{
    public Guid RunId { get; set; }
    public int TotalIterations { get; set; }
    public int SuccessfulIterations { get; set; }
    public int FailedIterations { get; set; }
    public long TotalDurationMs { get; set; }
    public double TotalDurationSeconds => TotalDurationMs / 1000.0;

    public double TpsPerIteration { get; set; }
    public double TpsPerStoredProcedure { get; set; }

    public double AverageLatencyMs { get; set; }
    public long MinLatencyMs { get; set; }
    public long MaxLatencyMs { get; set; }
    public long P95LatencyMs { get; set; }
    public long P99LatencyMs { get; set; }

    // New dynamic SP metrics: Key = SP Name, Value = Average Duration
    public Dictionary<string, double> AverageSpDurations { get; set; } = new();

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
