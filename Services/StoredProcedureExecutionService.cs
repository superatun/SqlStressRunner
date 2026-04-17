using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using SqlStressRunner.Models;

namespace SqlStressRunner.Services;

public class StoredProcedureExecutionService
{
    private readonly SqlConnectionFactory _connectionFactory;

    public StoredProcedureExecutionService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<StoredProcedureExecutionResult> ExecuteAsync(
        Guid iterationId,
        StoredProcedureConfiguration storedProcedure,
        string sql,
        Dictionary<string, object?> parameters,
        int commandTimeout,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new StoredProcedureExecutionResult
        {
            IterationId = iterationId,
            StoredProcedureName = storedProcedure.Name,
            StoredProcedureOrder = storedProcedure.Order,
            Parameters = new Dictionary<string, object?>(parameters),
            ExecutedAt = DateTime.UtcNow
        };

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = commandTimeout
            };

            ConfigureCommand(command, sql, parameters);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var payload = await ReadAllResultSetsAsync(reader, cancellationToken);

            result.ResponsePayload = JsonSerializer.Serialize(payload.ResultSets);
            result.ResponseRowCount = payload.TotalRowCount;
            result.ResponseResultSetCount = payload.ResultSets.Count;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionDurationMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    private static async Task<StoredProcedurePayloadResult> ReadAllResultSetsAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var resultSets = new List<object>();
        var totalRowCount = 0;
        var resultSetIndex = 0;

        do
        {
            if (reader.FieldCount <= 0)
            {
                continue;
            }

            resultSetIndex++;
            var rows = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value;
                }

                rows.Add(row);
            }

            totalRowCount += rows.Count;
            resultSets.Add(new
            {
                ResultSetIndex = resultSetIndex,
                Rows = rows
            });
        }
        while (await reader.NextResultAsync(cancellationToken));

        return new StoredProcedurePayloadResult
        {
            TotalRowCount = totalRowCount,
            ResultSets = resultSets
        };
    }

    private static void ConfigureCommand(SqlCommand command, string sql, Dictionary<string, object?> parameters)
    {
        if (sql.Trim().StartsWith("EXEC", StringComparison.OrdinalIgnoreCase) ||
            !sql.Contains(" "))
        {
            command.CommandType = CommandType.StoredProcedure;

            var spName = sql.Replace("EXEC", "", StringComparison.OrdinalIgnoreCase)
                .Trim()
                .Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0];

            command.CommandText = spName;

            var parameterMappings = ExtractParameterMappings(sql);
            foreach (var (spParamName, dictKey) in parameterMappings)
            {
                if (parameters.TryGetValue(dictKey, out var value))
                {
                    command.Parameters.AddWithValue(spParamName, value ?? DBNull.Value);
                }
            }

            return;
        }

        command.CommandType = CommandType.Text;
        var parameterDeclarations = BuildParameterDeclarations(parameters);
        command.CommandText = string.IsNullOrEmpty(parameterDeclarations)
            ? sql
            : parameterDeclarations + Environment.NewLine + sql;
    }

    private static string BuildParameterDeclarations(Dictionary<string, object?> parameters)
    {
        if (parameters.Count == 0)
        {
            return string.Empty;
        }

        var declarations = new System.Text.StringBuilder();
        foreach (var param in parameters)
        {
            var paramName = param.Key.StartsWith("@") ? param.Key : "@" + param.Key;
            if (param.Value == null)
            {
                declarations.AppendLine($"DECLARE {paramName} INT = NULL;");
                continue;
            }

            declarations.AppendLine($"DECLARE {paramName} {GetSqlType(param.Value)} = {ConvertToSqlLiteral(param.Value)};");
        }

        return declarations.ToString();
    }

    private static string GetSqlType(object value)
    {
        return value switch
        {
            int => "INT",
            long => "BIGINT",
            short => "SMALLINT",
            byte => "TINYINT",
            decimal => "DECIMAL(18,2)",
            double => "FLOAT",
            float => "REAL",
            bool => "BIT",
            DateTime => "DATETIME2",
            Guid => "UNIQUEIDENTIFIER",
            string s when s.Length <= 50 => "NVARCHAR(50)",
            string s when s.Length <= 255 => "NVARCHAR(255)",
            string => "NVARCHAR(MAX)",
            _ => "NVARCHAR(MAX)"
        };
    }

    private static string ConvertToSqlLiteral(object value)
    {
        return value switch
        {
            string s => $"N'{s.Replace("'", "''")}'",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'",
            Guid g => $"'{g}'",
            bool b => b ? "1" : "0",
            _ => value.ToString() ?? "NULL"
        };
    }

    private static List<(string spParamName, string dictKey)> ExtractParameterMappings(string sql)
    {
        var mappings = new List<(string, string)>();
        var execIndex = sql.IndexOf("EXEC", StringComparison.OrdinalIgnoreCase);
        if (execIndex == -1)
        {
            return mappings;
        }

        var afterExec = sql[(execIndex + 4)..].Trim();
        var lines = afterExec.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var parameterLines = string.Join(" ", lines.Skip(1));

        var assignmentRegex = new System.Text.RegularExpressions.Regex(
            @"(@\w+)\s*=\s*(@\w+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var matches = assignmentRegex.Matches(parameterLines);
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            mappings.Add((match.Groups[1].Value, match.Groups[2].Value));
        }

        if (mappings.Count > 0)
        {
            return mappings;
        }

        var simpleParamRegex = new System.Text.RegularExpressions.Regex(
            @"@\w+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in simpleParamRegex.Matches(parameterLines))
        {
            mappings.Add((match.Value, match.Value));
        }

        return mappings;
    }

    private sealed class StoredProcedurePayloadResult
    {
        public int TotalRowCount { get; set; }
        public List<object> ResultSets { get; set; } = new();
    }
}
