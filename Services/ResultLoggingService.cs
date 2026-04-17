using System.Text.Json;
using Microsoft.Data.SqlClient;
using SqlStressRunner.Models;

namespace SqlStressRunner.Services;

public class ResultLoggingService
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ResultLoggingService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task LogIterationAsync(Guid runId, IterationResult result, string legacyLogTableName)
    {
        try
        {
            var schemaNames = LoggingSchemaHelper.GetNames(legacyLogTableName);

            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await LogIterationHeaderAsync(connection, runId, result, schemaNames);
            await LogStoredProcedureResultsAsync(connection, runId, result, schemaNames);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error logging result: {ex.Message}");
        }
    }

    private static async Task LogIterationHeaderAsync(SqlConnection connection, Guid runId, IterationResult result, LoggingSchemaNames schemaNames)
    {
        var sql = $"""
            INSERT INTO {LoggingSchemaHelper.QuoteIdentifier(schemaNames.IterationTable)}
            (IterationId, RunId, IterationNumber, Success, TotalDurationMs, ExecutedAt, ErrorMessage)
            VALUES
            (@IterationId, @RunId, @IterationNumber, @Success, @TotalDurationMs, @ExecutedAt, @ErrorMessage);
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IterationId", result.IterationId);
        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@IterationNumber", result.IterationNumber);
        command.Parameters.AddWithValue("@Success", result.Success);
        command.Parameters.AddWithValue("@TotalDurationMs", result.TotalDurationMs);
        command.Parameters.AddWithValue("@ExecutedAt", result.ExecutedAt);
        command.Parameters.AddWithValue("@ErrorMessage", (object?)result.ErrorMessage ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task LogStoredProcedureResultsAsync(SqlConnection connection, Guid runId, IterationResult result, LoggingSchemaNames schemaNames)
    {
        var sql = $"""
            INSERT INTO {LoggingSchemaHelper.QuoteIdentifier(schemaNames.StoredProcedureTable)}
            (IterationId, RunId, IterationNumber, StoredProcedureName, StoredProcedureOrder, Success,
             ExecutionDurationMs, Parameters, ResponsePayload, ResponseRowCount, ResponseResultSetCount,
             ErrorMessage, ExecutedAt)
            VALUES
            (@IterationId, @RunId, @IterationNumber, @StoredProcedureName, @StoredProcedureOrder, @Success,
             @ExecutionDurationMs, @Parameters, @ResponsePayload, @ResponseRowCount, @ResponseResultSetCount,
             @ErrorMessage, @ExecutedAt);
            """;

        foreach (var storedProcedureResult in result.StoredProcedureResults)
        {
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IterationId", result.IterationId);
            command.Parameters.AddWithValue("@RunId", runId);
            command.Parameters.AddWithValue("@IterationNumber", result.IterationNumber);
            command.Parameters.AddWithValue("@StoredProcedureName", storedProcedureResult.StoredProcedureName);
            command.Parameters.AddWithValue("@StoredProcedureOrder", storedProcedureResult.StoredProcedureOrder);
            command.Parameters.AddWithValue("@Success", storedProcedureResult.Success);
            command.Parameters.AddWithValue("@ExecutionDurationMs", storedProcedureResult.ExecutionDurationMs);
            command.Parameters.AddWithValue("@Parameters", SerializeToJson(storedProcedureResult.Parameters));
            command.Parameters.AddWithValue("@ResponsePayload", (object?)storedProcedureResult.ResponsePayload ?? DBNull.Value);
            command.Parameters.AddWithValue("@ResponseRowCount", storedProcedureResult.ResponseRowCount);
            command.Parameters.AddWithValue("@ResponseResultSetCount", storedProcedureResult.ResponseResultSetCount);
            command.Parameters.AddWithValue("@ErrorMessage", (object?)storedProcedureResult.ErrorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@ExecutedAt", storedProcedureResult.ExecutedAt);
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task LogSummaryAsync(MetricsSummary summary, string legacyLogTableName)
    {
        try
        {
            var schemaNames = LoggingSchemaHelper.GetNames(legacyLogTableName);

            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var sql = $"""
                INSERT INTO {LoggingSchemaHelper.QuoteIdentifier(schemaNames.SummaryTable)}
                (RunId, TotalIterations, SuccessfulIterations, FailedIterations, TotalDurationMs,
                 TpsPerIteration, TpsPerStoredProcedure, AverageLatencyMs, MinLatencyMs, MaxLatencyMs,
                 P95LatencyMs, P99LatencyMs, StartTime, EndTime)
                VALUES
                (@RunId, @TotalIterations, @SuccessfulIterations, @FailedIterations, @TotalDurationMs,
                 @TpsPerIteration, @TpsPerStoredProcedure, @AverageLatencyMs, @MinLatencyMs, @MaxLatencyMs,
                 @P95LatencyMs, @P99LatencyMs, @StartTime, @EndTime);
                """;

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@RunId", summary.RunId);
            command.Parameters.AddWithValue("@TotalIterations", summary.TotalIterations);
            command.Parameters.AddWithValue("@SuccessfulIterations", summary.SuccessfulIterations);
            command.Parameters.AddWithValue("@FailedIterations", summary.FailedIterations);
            command.Parameters.AddWithValue("@TotalDurationMs", summary.TotalDurationMs);
            command.Parameters.AddWithValue("@TpsPerIteration", summary.TpsPerIteration);
            command.Parameters.AddWithValue("@TpsPerStoredProcedure", summary.TpsPerStoredProcedure);
            command.Parameters.AddWithValue("@AverageLatencyMs", summary.AverageLatencyMs);
            command.Parameters.AddWithValue("@MinLatencyMs", summary.MinLatencyMs);
            command.Parameters.AddWithValue("@MaxLatencyMs", summary.MaxLatencyMs);
            command.Parameters.AddWithValue("@P95LatencyMs", summary.P95LatencyMs);
            command.Parameters.AddWithValue("@P99LatencyMs", summary.P99LatencyMs);
            command.Parameters.AddWithValue("@StartTime", summary.StartTime);
            command.Parameters.AddWithValue("@EndTime", summary.EndTime);
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error logging summary: {ex.Message}");
        }
    }

    private static string SerializeToJson(object value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}
