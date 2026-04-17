using System.Linq;
using SqlStressRunner.Models;

namespace SqlStressRunner.Services;

public class MetricsService
{
    public MetricsSummary CalculateMetrics(List<IterationResult> results, DateTime startTime, DateTime endTime, int spCount = 2)
    {
        var totalDurationMs = (long)(endTime - startTime).TotalMilliseconds;
        var successfulResults = results.Where(r => r.Success).ToList();

        var metrics = new MetricsSummary
        {
            RunId = Guid.NewGuid(),
            TotalIterations = results.Count,
            SuccessfulIterations = successfulResults.Count,
            FailedIterations = results.Count - successfulResults.Count,
            TotalDurationMs = totalDurationMs,
            StartTime = startTime,
            EndTime = endTime
        };

        if (totalDurationMs > 0)
        {
            var totalSeconds = totalDurationMs / 1000.0;
            metrics.TpsPerIteration = successfulResults.Count / totalSeconds;
            metrics.TpsPerStoredProcedure = (successfulResults.Count * spCount) / totalSeconds;
        }

        if (successfulResults.Any())
        {
            var durations = successfulResults.Select(r => r.TotalDurationMs).OrderBy(d => d).ToList();

            metrics.AverageLatencyMs = durations.Average();
            metrics.MinLatencyMs = durations.Min();
            metrics.MaxLatencyMs = durations.Max();
            metrics.P95LatencyMs = CalculatePercentile(durations, 0.95);
            metrics.P99LatencyMs = CalculatePercentile(durations, 0.99);

            // Calculate average duration per SP using new dictionary format
            var allSpNames = successfulResults
                .SelectMany(r => r.StoredProcedureResults.Select(sp => sp.StoredProcedureName))
                .Distinct()
                .ToList();

            foreach (var spName in allSpNames)
            {
                var spDurations = successfulResults
                    .SelectMany(r => r.StoredProcedureResults)
                    .Where(sp => sp.Success && string.Equals(sp.StoredProcedureName, spName, StringComparison.Ordinal))
                    .Select(sp => sp.ExecutionDurationMs)
                    .ToList();

                if (spDurations.Any())
                {
                    metrics.AverageSpDurations[spName] = spDurations.Average();
                }
            }
        }

        return metrics;
    }

    private long CalculatePercentile(List<long> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
            return 0;

        var index = (int)Math.Ceiling(sortedValues.Count * percentile) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
        return sortedValues[index];
    }
}
