using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SqlStressRunner.ViewModels;

namespace SqlStressRunner.Views;

public partial class ParameterMappingView : UserControl
{
    public ParameterMappingView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ParameterMappingViewModel viewModel)
        {
            UpdateDataGridColumns(viewModel);
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ParameterMappingViewModel oldViewModel)
        {
            oldViewModel.StoredProcedureNamesChanged -= OnStoredProcedureNamesChanged;
        }

        if (e.NewValue is ParameterMappingViewModel newViewModel)
        {
            newViewModel.StoredProcedureNamesChanged += OnStoredProcedureNamesChanged;
            UpdateDataGridColumns(newViewModel);
        }
    }

    private void OnStoredProcedureNamesChanged(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("ParameterMappingView: OnStoredProcedureNamesChanged event received");
        if (DataContext is ParameterMappingViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine($"ParameterMappingView: Updating DataGrid columns. SP count: {viewModel.StoredProcedureNames.Count}");
            UpdateDataGridColumns(viewModel);
        }
    }

    private void UpdateDataGridColumns(ParameterMappingViewModel viewModel)
    {
        System.Diagnostics.Debug.WriteLine($"ParameterMappingView: UpdateDataGridColumns called with {viewModel.StoredProcedureNames.Count} SPs");

        MappingDataGrid.Columns.Clear();

        // Add the ColumnName column (read-only)
        var columnNameColumn = new DataGridTextColumn
        {
            Header = "Column Name",
            Binding = new Binding("ColumnName"),
            IsReadOnly = true,
            Width = new DataGridLength(200)
        };
        MappingDataGrid.Columns.Add(columnNameColumn);

        // Add a column for each stored procedure
        foreach (var spName in viewModel.StoredProcedureNames)
        {
            System.Diagnostics.Debug.WriteLine($"ParameterMappingView: Adding column for SP: {spName}");

            var column = new DataGridTextColumn
            {
                Header = $"{spName} Parameter",
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            };

            // Create a binding to the dictionary
            var binding = new Binding($"SpParameterMappings[{spName}]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            column.Binding = binding;

            MappingDataGrid.Columns.Add(column);
        }

        System.Diagnostics.Debug.WriteLine($"ParameterMappingView: DataGrid now has {MappingDataGrid.Columns.Count} columns");
    }
}
