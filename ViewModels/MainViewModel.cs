using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using SqlStressRunner.Commands;
using SqlStressRunner.Helpers;
using SqlStressRunner.Infrastructure;
using SqlStressRunner.Models;
using SqlStressRunner.Services;

namespace SqlStressRunner.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ConnectionViewModel _connectionViewModel;
    private TestConfigurationViewModel _testConfigurationViewModel;
    private ParameterMappingViewModel _parameterMappingViewModel;
    private ResultsViewModel _resultsViewModel;

    private int _selectedTabIndex;

    public MainViewModel()
    {
        _connectionViewModel = new ConnectionViewModel();
        _testConfigurationViewModel = new TestConfigurationViewModel();
        _parameterMappingViewModel = new ParameterMappingViewModel();
        _resultsViewModel = new ResultsViewModel();

        // Wire parameter mappings delegate to enable runtime retrieval
        _testConfigurationViewModel.GetParameterMappingsFunc = () => _parameterMappingViewModel.GetMappings();

        _connectionViewModel.ConnectionSettingsChanged += OnConnectionSettingsChanged;
        _testConfigurationViewModel.InitialDataLoaded += OnInitialDataLoaded;
        _testConfigurationViewModel.TestCompleted += OnTestCompleted;
        _testConfigurationViewModel.StoredProceduresChanged += OnStoredProceduresChanged;
        _testConfigurationViewModel.LogTableNameChanged += OnLogTableNameChanged;

        LoadConfigurationCommand = new AsyncRelayCommand(LoadConfigurationAsync);
        SaveConfigurationCommand = new AsyncRelayCommand(SaveConfigurationAsync);

        // Initialize SP names in parameter mapping
        var spNames = _testConfigurationViewModel.StoredProcedures.Select(sp => sp.Name).ToList();
        _parameterMappingViewModel.UpdateStoredProcedureNames(spNames);
        _connectionViewModel.LoggingTableBaseName = _testConfigurationViewModel.LogTableName;

        _ = LoadConfigurationAsync();
    }

    public ConnectionViewModel ConnectionViewModel
    {
        get => _connectionViewModel;
        set => SetProperty(ref _connectionViewModel, value);
    }

    public TestConfigurationViewModel TestConfigurationViewModel
    {
        get => _testConfigurationViewModel;
        set => SetProperty(ref _testConfigurationViewModel, value);
    }

    public ParameterMappingViewModel ParameterMappingViewModel
    {
        get => _parameterMappingViewModel;
        set => SetProperty(ref _parameterMappingViewModel, value);
    }

    public ResultsViewModel ResultsViewModel
    {
        get => _resultsViewModel;
        set => SetProperty(ref _resultsViewModel, value);
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public AsyncRelayCommand LoadConfigurationCommand { get; }
    public AsyncRelayCommand SaveConfigurationCommand { get; }

    private void OnConnectionSettingsChanged(object? sender, DatabaseConnectionSettings settings)
    {
        TestConfigurationViewModel.UpdateConnectionSettings(settings);
    }

    private void OnInitialDataLoaded(object? sender, (DataTable data, List<string> columns) args)
    {
        System.Diagnostics.Debug.WriteLine($"MainViewModel: OnInitialDataLoaded - Synchronizing SP names before initializing mappings");

        // Ensure Parameter Mapping has the latest SP names from Test Configuration
        var spNames = TestConfigurationViewModel.StoredProcedures.Select(sp => sp.Name).ToList();
        System.Diagnostics.Debug.WriteLine($"MainViewModel: Current SPs in TestConfiguration: {spNames.Count}");

        ParameterMappingViewModel.UpdateStoredProcedureNames(spNames);
        ParameterMappingViewModel.InitializeMappings(args.columns);
        TestConfigurationViewModel.SetDataset(args.data);
    }

    private void OnStoredProceduresChanged(object? sender, List<string> spNames)
    {
        System.Diagnostics.Debug.WriteLine($"MainViewModel: OnStoredProceduresChanged received with {spNames.Count} SPs");
        foreach (var name in spNames)
        {
            System.Diagnostics.Debug.WriteLine($"  - {name}");
        }
        ParameterMappingViewModel.UpdateStoredProcedureNames(spNames);
    }

    private void OnTestCompleted(object? sender, MetricsSummary summary)
    {
        ResultsViewModel.UpdateResults(summary);
        SelectedTabIndex = 3;
    }

    private void OnLogTableNameChanged(object? sender, string logTableName)
    {
        ConnectionViewModel.LoggingTableBaseName = logTableName;
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            var config = await ConfigurationHelper.LoadConfigurationAsync();
            if (config != null)
            {
                if (config.ConnectionSettings != null)
                {
                    ConnectionViewModel.LoadSettings(config.ConnectionSettings);
                }

                if (config.TestSettings != null)
                {
                    TestConfigurationViewModel.LoadSettings(config.TestSettings);
                }

                if (config.ParameterMappings != null)
                {
                    ParameterMappingViewModel.LoadMappings(config.ParameterMappings);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SaveConfigurationAsync()
    {
        try
        {
            var config = new AppConfiguration
            {
                ConnectionSettings = ConnectionViewModel.GetSettings(),
                TestSettings = TestConfigurationViewModel.GetSettings(),
                ParameterMappings = ParameterMappingViewModel.GetMappings()
            };

            await ConfigurationHelper.SaveConfigurationAsync(config);
            MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
