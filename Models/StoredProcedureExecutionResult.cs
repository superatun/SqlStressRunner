using System.Collections.Generic;

namespace SqlStressRunner.Models;

public class StoredProcedureExecutionResult
{
    public Guid IterationId { get; set; }
    public string StoredProcedureName { get; set; } = string.Empty;
    public int StoredProcedureOrder { get; set; }
    public bool Success { get; set; }
    public long ExecutionDurationMs { get; set; }
    public Dictionary<string, object?> Parameters { get; set; } = new();
    public string? ResponsePayload { get; set; }
    public int ResponseRowCount { get; set; }
    public int ResponseResultSetCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; }
}
