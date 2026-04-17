namespace SqlStressRunner.Models;

public class AppConfiguration
{
    public DatabaseConnectionSettings? ConnectionSettings { get; set; }
    public StressTestSettings? TestSettings { get; set; }
    public List<ParameterMapping>? ParameterMappings { get; set; }
}
