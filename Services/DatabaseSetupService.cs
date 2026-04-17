using Microsoft.Data.SqlClient;
using SqlStressRunner.Models;

namespace SqlStressRunner.Services;

public class DatabaseSetupService
{
    private readonly SqlConnectionFactory _connectionFactory;

    public DatabaseSetupService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<DatabaseSetupResult> SetupAsync(string? legacyLogTableName)
    {
        var schemaNames = LoggingSchemaHelper.GetNames(legacyLogTableName);
        var messages = new List<string>();

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var hasLegacyIterationTable = await TableExistsAsync(connection, schemaNames.LegacyIterationTable);
        var hasLegacySummaryTable = await TableExistsAsync(connection, schemaNames.SummaryTable);
        var hasNewIterationTable = await TableExistsAsync(connection, schemaNames.IterationTable);
        var hasNewStoredProcedureTable = await TableExistsAsync(connection, schemaNames.StoredProcedureTable);
        var hasNewSummaryTable = hasLegacySummaryTable && string.Equals(schemaNames.SummaryTable, "StressTestLogSummary", StringComparison.OrdinalIgnoreCase)
            ? await SummaryMatchesNewSchemaAsync(connection, schemaNames.SummaryTable)
            : hasLegacySummaryTable;

        if (!hasNewIterationTable)
        {
            await ExecuteNonQueryAsync(connection, BuildCreateIterationTableSql(schemaNames.IterationTable));
            messages.Add($"Created table {schemaNames.IterationTable}.");
        }
        else
        {
            messages.Add($"Table {schemaNames.IterationTable} already exists.");
        }

        if (!hasNewStoredProcedureTable)
        {
            await ExecuteNonQueryAsync(connection, BuildCreateStoredProcedureTableSql(schemaNames.StoredProcedureTable, schemaNames.IterationTable));
            messages.Add($"Created table {schemaNames.StoredProcedureTable}.");
        }
        else
        {
            messages.Add($"Table {schemaNames.StoredProcedureTable} already exists.");
        }

        if (!hasNewSummaryTable)
        {
            if (hasLegacySummaryTable)
            {
                await RenameTableAsync(connection, schemaNames.SummaryTable, schemaNames.LegacySummaryTable);
                messages.Add($"Renamed legacy summary table to {schemaNames.LegacySummaryTable}.");
                await ExecuteNonQueryAsync(connection, BuildCreateSummaryTableSql(schemaNames.SummaryTable));
                messages.Add($"Created table {schemaNames.SummaryTable}.");
                await MigrateLegacySummaryAsync(connection, schemaNames.LegacySummaryTable, schemaNames.SummaryTable);
                messages.Add($"Migrated data from {schemaNames.LegacySummaryTable} to {schemaNames.SummaryTable}.");
            }
            else
            {
                await ExecuteNonQueryAsync(connection, BuildCreateSummaryTableSql(schemaNames.SummaryTable));
                messages.Add($"Created table {schemaNames.SummaryTable}.");
            }
        }
        else
        {
            messages.Add($"Table {schemaNames.SummaryTable} already exists.");
        }

        if (hasLegacyIterationTable && !string.Equals(schemaNames.LegacyIterationTable, schemaNames.IterationTable, StringComparison.OrdinalIgnoreCase))
        {
            await MigrateLegacyIterationAsync(connection, schemaNames.LegacyIterationTable, schemaNames.IterationTable);
            messages.Add($"Migrated historical rows from {schemaNames.LegacyIterationTable} to {schemaNames.IterationTable}.");

            if (!await TableExistsAsync(connection, schemaNames.LegacyIterationArchiveTable))
            {
                await RenameTableAsync(connection, schemaNames.LegacyIterationTable, schemaNames.LegacyIterationArchiveTable);
                messages.Add($"Renamed legacy iteration table to {schemaNames.LegacyIterationArchiveTable}.");
            }
        }

        await RecreateSummaryViewAsync(connection, schemaNames);
        messages.Add($"Created or updated view {schemaNames.SummaryView}.");

        return new DatabaseSetupResult
        {
            Success = true,
            Message = string.Join(Environment.NewLine, messages)
        };
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @TableName
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TableName", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    private static async Task<bool> SummaryMatchesNewSchemaAsync(SqlConnection connection, string tableName)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = @TableName
              AND COLUMN_NAME IN ('AverageSp1DurationMs', 'AverageSp2DurationMs')
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TableName", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;
    }

    private static async Task RenameTableAsync(SqlConnection connection, string oldName, string newName)
    {
        if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var sql = $"EXEC sp_rename 'dbo.{oldName}', '{newName}';";
        await ExecuteNonQueryAsync(connection, sql);
    }

    private static async Task MigrateLegacyIterationAsync(SqlConnection connection, string legacyTableName, string newTableName)
    {
        var sql = $"""
            INSERT INTO {LoggingSchemaHelper.QuoteIdentifier(newTableName)}
            (IterationId, RunId, IterationNumber, Success, TotalDurationMs, ExecutedAt, ErrorMessage)
            SELECT NEWID(), legacy.RunId, legacy.IterationNumber, legacy.Success, legacy.TotalDurationMs, legacy.ExecutedAt, legacy.ErrorMessage
            FROM {LoggingSchemaHelper.QuoteIdentifier(legacyTableName)} legacy
            WHERE NOT EXISTS (
                SELECT 1
                FROM {LoggingSchemaHelper.QuoteIdentifier(newTableName)} currentRows
                WHERE currentRows.RunId = legacy.RunId
                  AND currentRows.IterationNumber = legacy.IterationNumber
            );
            """;

        await ExecuteNonQueryAsync(connection, sql);
    }

    private static async Task MigrateLegacySummaryAsync(SqlConnection connection, string legacyTableName, string newTableName)
    {
        var sql = $"""
            INSERT INTO {LoggingSchemaHelper.QuoteIdentifier(newTableName)}
            (RunId, TotalIterations, SuccessfulIterations, FailedIterations, TotalDurationMs,
             TpsPerIteration, TpsPerStoredProcedure, AverageLatencyMs, MinLatencyMs, MaxLatencyMs,
             P95LatencyMs, P99LatencyMs, StartTime, EndTime)
            SELECT legacy.RunId, legacy.TotalIterations, legacy.SuccessfulIterations, legacy.FailedIterations, legacy.TotalDurationMs,
                   legacy.TpsPerIteration, legacy.TpsPerStoredProcedure, legacy.AverageLatencyMs, legacy.MinLatencyMs, legacy.MaxLatencyMs,
                   legacy.P95LatencyMs, legacy.P99LatencyMs, legacy.StartTime, legacy.EndTime
            FROM {LoggingSchemaHelper.QuoteIdentifier(legacyTableName)} legacy
            WHERE NOT EXISTS (
                SELECT 1
                FROM {LoggingSchemaHelper.QuoteIdentifier(newTableName)} currentRows
                WHERE currentRows.RunId = legacy.RunId
            );
            """;

        await ExecuteNonQueryAsync(connection, sql);
    }

    private static async Task RecreateSummaryViewAsync(SqlConnection connection, LoggingSchemaNames schemaNames)
    {
        var dropSql = $"""
            IF OBJECT_ID('dbo.{schemaNames.SummaryView}', 'V') IS NOT NULL
                DROP VIEW dbo.{schemaNames.SummaryView};
            """;

        var createSql = $"""
            CREATE VIEW dbo.{schemaNames.SummaryView}
            AS
            SELECT
                s.RunId,
                s.TotalIterations,
                s.SuccessfulIterations,
                s.FailedIterations,
                s.TotalDurationMs / 1000.0 AS TotalDurationSeconds,
                s.TpsPerIteration,
                s.TpsPerStoredProcedure,
                s.AverageLatencyMs,
                s.MinLatencyMs,
                s.MaxLatencyMs,
                s.P95LatencyMs,
                s.P99LatencyMs,
                s.StartTime,
                s.EndTime,
                COUNT(i.IterationId) AS LoggedIterations
            FROM {LoggingSchemaHelper.QuoteIdentifier(schemaNames.SummaryTable)} s
            LEFT JOIN {LoggingSchemaHelper.QuoteIdentifier(schemaNames.IterationTable)} i ON s.RunId = i.RunId
            GROUP BY
                s.RunId, s.TotalIterations, s.SuccessfulIterations, s.FailedIterations,
                s.TotalDurationMs, s.TpsPerIteration, s.TpsPerStoredProcedure,
                s.AverageLatencyMs, s.MinLatencyMs, s.MaxLatencyMs,
                s.P95LatencyMs, s.P99LatencyMs, s.StartTime, s.EndTime;
            """;

        await ExecuteNonQueryAsync(connection, dropSql);
        await ExecuteNonQueryAsync(connection, createSql);
    }

    private static string BuildCreateIterationTableSql(string tableName)
    {
        return $"""
            CREATE TABLE {LoggingSchemaHelper.QuoteIdentifier(tableName)}
            (
                IterationId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                RunId UNIQUEIDENTIFIER NOT NULL,
                IterationNumber INT NOT NULL,
                Success BIT NOT NULL,
                TotalDurationMs BIGINT NOT NULL,
                ExecutedAt DATETIME2 NOT NULL,
                ErrorMessage NVARCHAR(MAX) NULL
            );
            CREATE INDEX IX_{tableName}_RunId ON {LoggingSchemaHelper.QuoteIdentifier(tableName)} (RunId);
            CREATE INDEX IX_{tableName}_IterationNumber ON {LoggingSchemaHelper.QuoteIdentifier(tableName)} (IterationNumber);
            CREATE INDEX IX_{tableName}_ExecutedAt ON {LoggingSchemaHelper.QuoteIdentifier(tableName)} (ExecutedAt);
            """;
    }

    private static string BuildCreateStoredProcedureTableSql(string tableName, string iterationTableName)
    {
        return $"""
            CREATE TABLE {LoggingSchemaHelper.QuoteIdentifier(tableName)}
            (
                Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                IterationId UNIQUEIDENTIFIER NOT NULL,
                RunId UNIQUEIDENTIFIER NOT NULL,
                IterationNumber INT NOT NULL,
                StoredProcedureName NVARCHAR(200) NOT NULL,
                StoredProcedureOrder INT NOT NULL,
                Success BIT NOT NULL,
                ExecutionDurationMs BIGINT NOT NULL,
                Parameters NVARCHAR(MAX) NULL,
                ResponsePayload NVARCHAR(MAX) NULL,
                ResponseRowCount INT NULL,
                ResponseResultSetCount INT NULL,
                ErrorMessage NVARCHAR(MAX) NULL,
                ExecutedAt DATETIME2 NOT NULL,
                CONSTRAINT FK_{tableName}_{iterationTableName}
                    FOREIGN KEY (IterationId) REFERENCES {LoggingSchemaHelper.QuoteIdentifier(iterationTableName)}(IterationId)
            );
            CREATE INDEX IX_{tableName}_IterationId ON {LoggingSchemaHelper.QuoteIdentifier(tableName)} (IterationId);
            CREATE INDEX IX_{tableName}_RunId ON {LoggingSchemaHelper.QuoteIdentifier(tableName)} (RunId);
            CREATE INDEX IX_{tableName}_StoredProcedureName ON {LoggingSchemaHelper.QuoteIdentifier(tableName)} (StoredProcedureName);
            CREATE INDEX IX_{tableName}_ExecutedAt ON {LoggingSchemaHelper.QuoteIdentifier(tableName)} (ExecutedAt);
            """;
    }

    private static string BuildCreateSummaryTableSql(string tableName)
    {
        return $"""
            CREATE TABLE {LoggingSchemaHelper.QuoteIdentifier(tableName)}
            (
                Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                RunId UNIQUEIDENTIFIER NOT NULL UNIQUE,
                TotalIterations INT NOT NULL,
                SuccessfulIterations INT NOT NULL,
                FailedIterations INT NOT NULL,
                TotalDurationMs BIGINT NOT NULL,
                TpsPerIteration FLOAT NOT NULL,
                TpsPerStoredProcedure FLOAT NOT NULL,
                AverageLatencyMs FLOAT NOT NULL,
                MinLatencyMs BIGINT NOT NULL,
                MaxLatencyMs BIGINT NOT NULL,
                P95LatencyMs BIGINT NOT NULL,
                P99LatencyMs BIGINT NOT NULL,
                StartTime DATETIME2 NOT NULL,
                EndTime DATETIME2 NOT NULL
            );
            CREATE INDEX IX_{tableName}_StartTime ON {LoggingSchemaHelper.QuoteIdentifier(tableName)} (StartTime);
            """;
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection connection, string sql)
    {
        using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

public class DatabaseSetupResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
