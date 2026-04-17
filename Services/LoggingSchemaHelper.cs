using System.Text.RegularExpressions;

namespace SqlStressRunner.Services;

public static class LoggingSchemaHelper
{
    private static readonly Regex SafeIdentifierRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public static LoggingSchemaNames GetNames(string? legacyLogTableName)
    {
        var baseName = string.IsNullOrWhiteSpace(legacyLogTableName) ? "StressTestLog" : legacyLogTableName.Trim();
        ValidateIdentifier(baseName);

        if (string.Equals(baseName, "StressTestLog", StringComparison.OrdinalIgnoreCase))
        {
            return new LoggingSchemaNames(
                "StressTestIterationLog",
                "StressTestStoredProcedureLog",
                "StressTestLogSummary",
                "vw_StressTestSummary",
                "StressTestLog",
                "StressTestLogLegacy",
                "StressTestLogSummaryLegacy");
        }

        return new LoggingSchemaNames(
            $"{baseName}Iteration",
            $"{baseName}StoredProcedure",
            $"{baseName}Summary",
            $"vw_{baseName}Summary",
            baseName,
            $"{baseName}Legacy",
            $"{baseName}SummaryLegacy");
    }

    public static string QuoteIdentifier(string identifier)
    {
        ValidateIdentifier(identifier);
        return $"[{identifier}]";
    }

    public static void ValidateIdentifier(string identifier)
    {
        if (!SafeIdentifierRegex.IsMatch(identifier))
        {
            throw new InvalidOperationException($"Unsafe SQL identifier: {identifier}");
        }
    }
}

public record LoggingSchemaNames(
    string IterationTable,
    string StoredProcedureTable,
    string SummaryTable,
    string SummaryView,
    string LegacyIterationTable,
    string LegacyIterationArchiveTable,
    string LegacySummaryTable);
