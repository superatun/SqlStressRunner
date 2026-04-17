using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SqlStressRunner.Infrastructure;
using SqlStressRunner.Models;

namespace SqlStressRunner.ViewModels;

public class ParameterMappingViewModel : ViewModelBase
{
    private ObservableCollection<ParameterMapping> _mappings = new();
    private List<string> _storedProcedureNames = new();

    public ParameterMappingViewModel()
    {
    }

    public ObservableCollection<ParameterMapping> Mappings
    {
        get => _mappings;
        set => SetProperty(ref _mappings, value);
    }

    public List<string> StoredProcedureNames
    {
        get => _storedProcedureNames;
        set 
        {
            if (SetProperty(ref _storedProcedureNames, value))
            {
                OnStoredProcedureNamesChanged();
            }
        }
    }

    public event EventHandler? StoredProcedureNamesChanged;

    private void OnStoredProcedureNamesChanged()
    {
        StoredProcedureNamesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void InitializeMappings(List<string> columnNames)
    {
        System.Diagnostics.Debug.WriteLine($"InitializeMappings called with {columnNames.Count} columns");
        System.Diagnostics.Debug.WriteLine($"StoredProcedureNames count: {StoredProcedureNames.Count}");
        foreach (var sp in StoredProcedureNames)
        {
            System.Diagnostics.Debug.WriteLine($"  - SP: {sp}");
        }

        Mappings.Clear();
        foreach (var column in columnNames)
        {
            var mapping = new ParameterMapping
            {
                ColumnName = column
            };

            // Auto-initialize SpParameterMappings with @columnName for each SP
            foreach (var spName in StoredProcedureNames)
            {
                // Auto-complete with column name prefixed with @
                var defaultParamName = column.StartsWith("@") ? column : $"@{column}";
                mapping.SpParameterMappings[spName] = defaultParamName;
                System.Diagnostics.Debug.WriteLine($"  Auto-initialized mapping: {column} -> {spName} = {defaultParamName}");
            }

            // Backward compatibility: if we have exactly 2 SPs, populate legacy properties
            if (StoredProcedureNames.Count >= 1 && mapping.SpParameterMappings.TryGetValue(StoredProcedureNames[0], out var param1))
                mapping.Sp1ParameterName = param1;
            if (StoredProcedureNames.Count >= 2 && mapping.SpParameterMappings.TryGetValue(StoredProcedureNames[1], out var param2))
                mapping.Sp2ParameterName = param2;

            Mappings.Add(mapping);
        }

        System.Diagnostics.Debug.WriteLine($"Firing OnStoredProcedureNamesChanged event");
        OnStoredProcedureNamesChanged();
    }

    public void UpdateStoredProcedureNames(List<string> spNames)
    {
        System.Diagnostics.Debug.WriteLine($"ParameterMappingViewModel: UpdateStoredProcedureNames called with {spNames.Count} SPs");
        foreach (var name in spNames)
        {
            System.Diagnostics.Debug.WriteLine($"  - {name}");
        }

        StoredProcedureNames = spNames;

        // Update existing mappings to include new SPs or remove deleted ones
        foreach (var mapping in Mappings)
        {
            // Remove mappings for deleted SPs
            var toRemove = mapping.SpParameterMappings.Keys
                .Where(key => !spNames.Contains(key))
                .ToList();

            foreach (var key in toRemove)
            {
                mapping.SpParameterMappings.Remove(key);
            }

            // Add mappings for new SPs with auto-completed parameter names
            foreach (var spName in spNames)
            {
                if (!mapping.SpParameterMappings.ContainsKey(spName))
                {
                    // Auto-complete with column name prefixed with @
                    var defaultParamName = mapping.ColumnName.StartsWith("@") 
                        ? mapping.ColumnName 
                        : $"@{mapping.ColumnName}";
                    mapping.SpParameterMappings[spName] = defaultParamName;
                    System.Diagnostics.Debug.WriteLine($"  Auto-initialized: {mapping.ColumnName} -> {spName} = {defaultParamName}");
                }
            }

            // Sync dictionary to legacy properties for backward compatibility
            if (spNames.Count >= 1 && mapping.SpParameterMappings.TryGetValue(spNames[0], out var param1))
                mapping.Sp1ParameterName = param1;
            if (spNames.Count >= 2 && mapping.SpParameterMappings.TryGetValue(spNames[1], out var param2))
                mapping.Sp2ParameterName = param2;
        }
    }

    public List<ParameterMapping> GetMappings()
    {
        // Sync dictionary values to legacy properties for backward compatibility
        foreach (var mapping in Mappings)
        {
            if (StoredProcedureNames.Count >= 1 && mapping.SpParameterMappings.TryGetValue(StoredProcedureNames[0], out var param1))
            {
                mapping.Sp1ParameterName = param1;
            }
            if (StoredProcedureNames.Count >= 2 && mapping.SpParameterMappings.TryGetValue(StoredProcedureNames[1], out var param2))
            {
                mapping.Sp2ParameterName = param2;
            }
        }

        return Mappings.ToList();
    }

    public void LoadMappings(List<ParameterMapping> mappings)
    {
        Mappings.Clear();
        foreach (var mapping in mappings)
        {
            // Ensure SpParameterMappings is populated
            if (!mapping.SpParameterMappings.Any() && StoredProcedureNames.Any())
            {
                // Migrate from legacy format
                if (StoredProcedureNames.Count >= 1 && !string.IsNullOrEmpty(mapping.Sp1ParameterName))
                {
                    mapping.SpParameterMappings[StoredProcedureNames[0]] = mapping.Sp1ParameterName;
                }
                if (StoredProcedureNames.Count >= 2 && !string.IsNullOrEmpty(mapping.Sp2ParameterName))
                {
                    mapping.SpParameterMappings[StoredProcedureNames[1]] = mapping.Sp2ParameterName;
                }
            }

            Mappings.Add(mapping);
        }
    }
}
