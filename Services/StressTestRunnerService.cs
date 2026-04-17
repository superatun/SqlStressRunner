using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Linq;
using SqlStressRunner.Models;

namespace SqlStressRunner.Services;

public class StressTestRunnerService
{
    private readonly StoredProcedureExecutionService _spExecutionService;
    private readonly MetricsService _metricsService;
    private readonly ResultLoggingService? _loggingService;

    public StressTestRunnerService(
        SqlConnectionFactory connectionFactory,
        StoredProcedureExecutionService spExecutionService,
        MetricsService metricsService,
        ResultLoggingService? loggingService = null)
    {
        _spExecutionService = spExecutionService ?? throw new ArgumentNullException(nameof(spExecutionService));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _loggingService = loggingService;
    }

    public async Task<MetricsSummary> RunTestAsync(
        StressTestSettings settings,
        DataTable dataset,
        List<ParameterMapping> parameterMappings,
        IProgress<(int current, int total, TestRunState state)>? progress,
        CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid();
        var results = new ConcurrentBag<IterationResult>();
        var startTime = DateTime.UtcNow;

        // Get enabled SPs ordered by their execution order
        var enabledSPs = settings.StoredProcedures
            .Where(sp => sp.IsEnabled)
            .OrderBy(sp => sp.Order)
            .ToList();

        // Fallback to legacy properties if no SPs in collection
        if (!enabledSPs.Any())
        {
            if (!string.IsNullOrEmpty(settings.StoredProcedure1))
            {
                enabledSPs.Add(new StoredProcedureConfiguration
                {
                    Name = "SP1",
                    SqlCommand = settings.StoredProcedure1,
                    Order = 1,
                    IsEnabled = true
                });
            }
            if (!string.IsNullOrEmpty(settings.StoredProcedure2))
            {
                enabledSPs.Add(new StoredProcedureConfiguration
                {
                    Name = "SP2",
                    SqlCommand = settings.StoredProcedure2,
                    Order = 2,
                    IsEnabled = true
                });
            }
        }

        if (!enabledSPs.Any())
        {
            throw new InvalidOperationException("No stored procedures configured for execution.");
        }

        try
        {
            var totalIterations = settings.TotalIterations;
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            };

            var iterationCounter = 0;

            await Parallel.ForEachAsync(
                Enumerable.Range(0, totalIterations),
                parallelOptions,
                async (iteration, ct) =>
                {
                    var rowIndex = settings.RecycleDataset
                        ? iteration % dataset.Rows.Count
                        : iteration;

                    if (rowIndex >= dataset.Rows.Count)
                        return;

                    var row = dataset.Rows[rowIndex];
                    var result = await ExecuteIterationAsync(
                        iteration,
                        row,
                        enabledSPs,
                        settings,
                        parameterMappings,
                        ct);

                    results.Add(result);

                    if (settings.LogToDatabase && _loggingService != null)
                    {
                        await _loggingService.LogIterationAsync(runId, result, settings.LogTableName);
                    }

                    var current = Interlocked.Increment(ref iterationCounter);
                    progress?.Report((current, totalIterations, TestRunState.Running));
                });

            var endTime = DateTime.UtcNow;
            var summary = _metricsService.CalculateMetrics(results.ToList(), startTime, endTime, enabledSPs.Count);
            summary.RunId = runId;

            if (settings.LogToDatabase && _loggingService != null)
            {
                await _loggingService.LogSummaryAsync(summary, settings.LogTableName);
            }

            progress?.Report((totalIterations, totalIterations, TestRunState.Completed));
            return summary;
        }
        catch (OperationCanceledException)
        {
            var endTime = DateTime.UtcNow;
            var summary = _metricsService.CalculateMetrics(results.ToList(), startTime, endTime, enabledSPs.Count);
            summary.RunId = runId;
            progress?.Report((results.Count, settings.TotalIterations, TestRunState.Cancelled));
            return summary;
        }
        catch (Exception)
        {
            var endTime = DateTime.UtcNow;
            var summary = _metricsService.CalculateMetrics(results.ToList(), startTime, endTime, enabledSPs.Count);
            summary.RunId = runId;
            progress?.Report((results.Count, settings.TotalIterations, TestRunState.Failed));
            throw;
        }
    }

    private async Task<IterationResult> ExecuteIterationAsync(
        int iterationNumber,
        DataRow row,
        List<StoredProcedureConfiguration> storedProcedures,
        StressTestSettings settings,
        List<ParameterMapping> parameterMappings,
        CancellationToken cancellationToken)
    {
        var result = new IterationResult
        {
            IterationId = Guid.NewGuid(),
            IterationNumber = iterationNumber,
            ExecutedAt = DateTime.UtcNow,
            Success = true // Assume success unless a failure occurs
        };

        var totalStopwatch = Stopwatch.StartNew();

        try
        {
            // Execute each stored procedure in order
            foreach (var sp in storedProcedures)
            {
                var spParams = BuildParameters(row, parameterMappings, sp.Name);

                var spResult = await _spExecutionService.ExecuteAsync(
                    result.IterationId,
                    sp,
                    sp.SqlCommand,
                    spParams,
                    settings.CommandTimeout,
                    cancellationToken);

                result.StoredProcedureResults.Add(spResult);

                if (!spResult.Success)
                {
                    result.ErrorMessage = $"{sp.Name} failed: {spResult.ErrorMessage}";
                    result.Success = false;
                    break; // Stop executing remaining SPs if one fails
                }
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Iteration failed: {ex.Message}";
            result.Success = false;
        }
        finally
        {
            totalStopwatch.Stop();
            result.TotalDurationMs = totalStopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    private Dictionary<string, object?> BuildParameters(
        DataRow row,
        List<ParameterMapping> mappings,
        string spName)
    {
        var parameters = new Dictionary<string, object?>();

        foreach (var mapping in mappings)
        {
            // Try to get parameter from new dictionary format
            string? paramName = null;
            if (mapping.SpParameterMappings.TryGetValue(spName, out var mappedParam))
            {
                paramName = mappedParam;
            }
            else
            {
                // Fallback to legacy properties for backward compatibility
                if (spName == "SP1")
                    paramName = mapping.Sp1ParameterName;
                else if (spName == "SP2")
                    paramName = mapping.Sp2ParameterName;
            }

            if (string.IsNullOrWhiteSpace(paramName))
                continue;

            if (!paramName.StartsWith("@"))
                paramName = "@" + paramName;

            if (row.Table.Columns.Contains(mapping.ColumnName))
            {
                parameters[paramName] = row[mapping.ColumnName];
            }
        }

        return parameters;
    }
}
