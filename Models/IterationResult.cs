namespace SqlStressRunner.Models;

public class IterationResult
{
    public Guid IterationId { get; set; }
    public int IterationNumber { get; set; }
    public bool Success { get; set; }
    public long TotalDurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; }
    public List<StoredProcedureExecutionResult> StoredProcedureResults { get; set; } = new();
}
