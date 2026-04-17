using System.Text;
using SqlStressRunner.Models;

namespace SqlStressRunner.Services;

public class ReportGenerationService
{
    public string GenerateMarkdownReport(MetricsSummary summary)
    {
        var builder = new StringBuilder();
        var generatedAt = DateTime.UtcNow;

        builder.AppendLine("# SQL Stress Runner Report");
        builder.AppendLine();
        builder.AppendLine($"Generated at (UTC): {generatedAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"RunId: `{summary.RunId}`");
        builder.AppendLine();

        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Total iterations: {summary.TotalIterations}");
        builder.AppendLine($"- Successful iterations: {summary.SuccessfulIterations}");
        builder.AppendLine($"- Failed iterations: {summary.FailedIterations}");
        builder.AppendLine($"- Total duration: {summary.TotalDurationMs} ms ({summary.TotalDurationSeconds:F2} s)");
        builder.AppendLine($"- TPS per iteration: {summary.TpsPerIteration:F2}");
        builder.AppendLine($"- TPS per command: {summary.TpsPerStoredProcedure:F2}");
        builder.AppendLine($"- Average latency: {summary.AverageLatencyMs:F2} ms");
        builder.AppendLine($"- Min latency: {summary.MinLatencyMs} ms");
        builder.AppendLine($"- Max latency: {summary.MaxLatencyMs} ms");
        builder.AppendLine($"- P95 latency: {summary.P95LatencyMs} ms");
        builder.AppendLine($"- P99 latency: {summary.P99LatencyMs} ms");
        builder.AppendLine();

        builder.AppendLine("## Average Duration Per Command");
        builder.AppendLine();
        builder.AppendLine("| Command | Average Duration (ms) |");
        builder.AppendLine("| --- | ---: |");

        foreach (var item in summary.AverageSpDurations.OrderBy(kvp => kvp.Key))
        {
            builder.AppendLine($"| {EscapePipe(item.Key)} | {item.Value:F2} |");
        }

        builder.AppendLine();

        var iterationResults = summary.IterationResults
            .OrderBy(r => r.IterationNumber)
            .ToList();

        builder.AppendLine("## Iteration Overview");
        builder.AppendLine();
        builder.AppendLine("| Iteration | IterationId | Success | Total Duration (ms) | Error |");
        builder.AppendLine("| ---: | --- | --- | ---: | --- |");

        foreach (var iteration in iterationResults)
        {
            builder.AppendLine(
                $"| {iteration.IterationNumber} | `{iteration.IterationId}` | {(iteration.Success ? "Yes" : "No")} | {iteration.TotalDurationMs} | {EscapePipe(iteration.ErrorMessage)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Command Detail");
        builder.AppendLine();
        builder.AppendLine("| Iteration | Command Order | Command | Success | Duration (ms) | Rows | Result Sets | Error |");
        builder.AppendLine("| ---: | ---: | --- | --- | ---: | ---: | ---: | --- |");

        foreach (var iteration in iterationResults)
        {
            foreach (var command in iteration.StoredProcedureResults.OrderBy(r => r.StoredProcedureOrder))
            {
                builder.AppendLine(
                    $"| {iteration.IterationNumber} | {command.StoredProcedureOrder} | {EscapePipe(command.StoredProcedureName)} | {(command.Success ? "Yes" : "No")} | {command.ExecutionDurationMs} | {command.ResponseRowCount} | {command.ResponseResultSetCount} | {EscapePipe(command.ErrorMessage)} |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Failed Commands");
        builder.AppendLine();

        var failedCommands = iterationResults
            .SelectMany(iteration => iteration.StoredProcedureResults.Select(command => new { iteration.IterationNumber, Command = command }))
            .Where(x => !x.Command.Success)
            .ToList();

        if (failedCommands.Count == 0)
        {
            builder.AppendLine("No failed commands were recorded in this run.");
            builder.AppendLine();
        }
        else
        {
            foreach (var failed in failedCommands)
            {
                builder.AppendLine($"### Iteration {failed.IterationNumber} - {failed.Command.StoredProcedureName}");
                builder.AppendLine();
                builder.AppendLine($"- Duration: {failed.Command.ExecutionDurationMs} ms");
                builder.AppendLine($"- Error: {failed.Command.ErrorMessage}");
                builder.AppendLine($"- Parameters: `{failed.Command.ParametersAsJson()}`");
                builder.AppendLine();
            }
        }

        builder.AppendLine("## Response Payload Notes");
        builder.AppendLine();
        builder.AppendLine("The in-app report summarizes row counts and result set counts. Full response payloads remain available in the database logging tables when logging is enabled.");

        return builder.ToString();
    }

    private static string EscapePipe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Replace("|", "\\|").Replace(Environment.NewLine, "<br/>");
    }
}

internal static class StoredProcedureExecutionResultReportExtensions
{
    public static string ParametersAsJson(this StoredProcedureExecutionResult result)
    {
        return System.Text.Json.JsonSerializer.Serialize(result.Parameters);
    }
}
