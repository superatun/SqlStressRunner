namespace SqlStressRunner.Models;

public class StoredProcedureConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string SqlCommand { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsEnabled { get; set; } = true;
}
