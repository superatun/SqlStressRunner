using System.Collections.ObjectModel;

namespace SqlStressRunner.Models;

public class StressTestSettings
{
    public string InitialQuery { get; set; } = string.Empty;

    // Legacy properties for backward compatibility
    public string StoredProcedure1 { get; set; } = string.Empty;
    public string StoredProcedure2 { get; set; } = string.Empty;

    // New dynamic stored procedures list
    public ObservableCollection<StoredProcedureConfiguration> StoredProcedures { get; set; } = new();

    public int CommandTimeout { get; set; } = 30;
    public int MaxDegreeOfParallelism { get; set; } = 1;
    public int TotalIterations { get; set; } = 100;
    public bool RecycleDataset { get; set; } = true;
    public bool LogToDatabase { get; set; }
    public string LogTableName { get; set; } = "StressTestLog";
}
