using System.Collections.Generic;
using System.Collections.ObjectModel;
using SqlStressRunner.Infrastructure;

namespace SqlStressRunner.Models;

public class ParameterMapping
{
    public string ColumnName { get; set; } = string.Empty;

    // Legacy properties for backward compatibility
    public string Sp1ParameterName { get; set; } = string.Empty;
    public string Sp2ParameterName { get; set; } = string.Empty;

    // New dynamic parameter mappings: Key = SP Name, Value = Parameter Name
    public ObservableDictionary<string, string> SpParameterMappings { get; set; } = new();
}

public class ParameterMappingCollection
{
    public ObservableCollection<ParameterMapping> Mappings { get; set; } = new();
}
