namespace SqlStressRunner.Models;

public class StoredProcedureMetricSummary
{
    public string StoredProcedureName { get; set; } = string.Empty;
    public double AverageDurationMs { get; set; }
}
