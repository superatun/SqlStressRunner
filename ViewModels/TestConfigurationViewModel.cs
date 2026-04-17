using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using SqlStressRunner.Commands;
using SqlStressRunner.Infrastructure;
using SqlStressRunner.Models;
using SqlStressRunner.Services;

namespace SqlStressRunner.ViewModels;

public class TestConfigurationViewModel : ViewModelBase
{
    private DatabaseConnectionSettings? _connectionSettings;
    private DataTable? _dataset;
    private CancellationTokenSource? _cancellationTokenSource;

    private string _initialQuery = "SELECT TOP 100 * FROM YourTable";
    private int _commandTimeout = 30;
    private int _maxDegreeOfParallelism = 1;
    private int _totalIterations = 100;
    private bool _recycleDataset = true;
    private bool _logToDatabase;
    private string _logTableName = "StressTestLog";
    private bool _isRunning;
    private int _currentIteration;
    private string _statusMessage = string.Empty;
    private ObservableCollection<StoredProcedureConfiguration> _storedProcedures = new();

    public event EventHandler<(DataTable data, List<string> columns)>? InitialDataLoaded;
    public event EventHandler<MetricsSummary>? TestCompleted;
    public event EventHandler<string>? LogTableNameChanged;

    public TestConfigurationViewModel()
    {
        // StoredProcedures collection starts empty - user must add SPs manually or load from config

        LoadInitialDataCommand = new AsyncRelayCommand(LoadInitialDataAsync, () => !IsRunning);
        StartTestCommand = new AsyncRelayCommand(StartTestAsync, () => !IsRunning && _dataset != null);
        CancelTestCommand = new RelayCommand(CancelTest, () => IsRunning);
        AddStoredProcedureCommand = new RelayCommand(AddStoredProcedure);
        RemoveStoredProcedureCommand = new RelayCommand<StoredProcedureConfiguration>(RemoveStoredProcedure);
        MoveUpCommand = new RelayCommand<StoredProcedureConfiguration>(MoveUp);
        MoveDownCommand = new RelayCommand<StoredProcedureConfiguration>(MoveDown);
    }

    public string InitialQuery
    {
        get => _initialQuery;
        set => SetProperty(ref _initialQuery, value);
    }

    public ObservableCollection<StoredProcedureConfiguration> StoredProcedures
    {
        get => _storedProcedures;
        set => SetProperty(ref _storedProcedures, value);
    }

    public int CommandTimeout
    {
        get => _commandTimeout;
        set => SetProperty(ref _commandTimeout, value);
    }

    public int MaxDegreeOfParallelism
    {
        get => _maxDegreeOfParallelism;
        set => SetProperty(ref _maxDegreeOfParallelism, value);
    }

    public int TotalIterations
    {
        get => _totalIterations;
        set => SetProperty(ref _totalIterations, value);
    }

    public bool RecycleDataset
    {
        get => _recycleDataset;
        set => SetProperty(ref _recycleDataset, value);
    }

    public bool LogToDatabase
    {
        get => _logToDatabase;
        set => SetProperty(ref _logToDatabase, value);
    }

    public string LogTableName
    {
        get => _logTableName;
        set
        {
            if (SetProperty(ref _logTableName, value))
            {
                LogTableNameChanged?.Invoke(this, value);
            }
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetProperty(ref _isRunning, value))
            {
                LoadInitialDataCommand.RaiseCanExecuteChanged();
                StartTestCommand.RaiseCanExecuteChanged();
                CancelTestCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int CurrentIteration
    {
        get => _currentIteration;
        set => SetProperty(ref _currentIteration, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public double ProgressPercentage => TotalIterations > 0 ? (double)CurrentIteration / TotalIterations * 100 : 0;

    public AsyncRelayCommand LoadInitialDataCommand { get; }
    public AsyncRelayCommand StartTestCommand { get; }
    public RelayCommand CancelTestCommand { get; }
    public RelayCommand AddStoredProcedureCommand { get; }
    public RelayCommand<StoredProcedureConfiguration> RemoveStoredProcedureCommand { get; }
    public RelayCommand<StoredProcedureConfiguration> MoveUpCommand { get; }
    public RelayCommand<StoredProcedureConfiguration> MoveDownCommand { get; }

    private void AddStoredProcedure()
    {
        var newOrder = StoredProcedures.Any() ? StoredProcedures.Max(sp => sp.Order) + 1 : 1;
        StoredProcedures.Add(new StoredProcedureConfiguration
        {
            Name = $"SP{newOrder}",
            SqlCommand = $"EXEC YourSP{newOrder} @Param",
            Order = newOrder,
            IsEnabled = true
        });
        OnStoredProceduresChanged();
    }

    private void RemoveStoredProcedure(StoredProcedureConfiguration? sp)
    {
        if (sp != null)
        {
            StoredProcedures.Remove(sp);
            ReorderStoredProcedures();
            OnStoredProceduresChanged();
        }
    }

    private void MoveUp(StoredProcedureConfiguration? sp)
    {
        if (sp == null) return;
        
        var index = StoredProcedures.IndexOf(sp);
        if (index > 0)
        {
            StoredProcedures.Move(index, index - 1);
            ReorderStoredProcedures();
            OnStoredProceduresChanged();
        }
    }

    private void MoveDown(StoredProcedureConfiguration? sp)
    {
        if (sp == null) return;
        
        var index = StoredProcedures.IndexOf(sp);
        if (index < StoredProcedures.Count - 1)
        {
            StoredProcedures.Move(index, index + 1);
            ReorderStoredProcedures();
            OnStoredProceduresChanged();
        }
    }

    private void ReorderStoredProcedures()
    {
        for (int i = 0; i < StoredProcedures.Count; i++)
        {
            StoredProcedures[i].Order = i + 1;
        }
    }

    private void OnStoredProceduresChanged()
    {
        var spNames = StoredProcedures.Select(sp => sp.Name).ToList();
        System.Diagnostics.Debug.WriteLine($"TestConfigurationViewModel: OnStoredProceduresChanged - Firing event with {spNames.Count} SPs");
        foreach (var name in spNames)
        {
            System.Diagnostics.Debug.WriteLine($"  - {name}");
        }
        // Notify parameter mapping that SP list has changed
        StoredProceduresChanged?.Invoke(this, spNames);
    }

    public event EventHandler<List<string>>? StoredProceduresChanged;

    public void UpdateConnectionSettings(DatabaseConnectionSettings settings)
    {
        _connectionSettings = settings;
    }

    public void SetDataset(DataTable dataset)
    {
        _dataset = dataset;
        StartTestCommand.RaiseCanExecuteChanged();
    }

    private async Task LoadInitialDataAsync()
    {
        if (_connectionSettings == null)
        {
            MessageBox.Show("Please configure and save connection settings first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            StatusMessage = "Loading initial data...";
            var factory = new SqlConnectionFactory(_connectionSettings);
            var loader = new InitialDataLoaderService(factory);

            var data = await loader.LoadDataAsync(InitialQuery, CommandTimeout);
            var columns = loader.GetColumnNames(data);

            _dataset = data;
            StatusMessage = $"Loaded {data.Rows.Count} rows with {columns.Count} columns.";

            InitialDataLoaded?.Invoke(this, (data, columns));
            StartTestCommand.RaiseCanExecuteChanged();

            MessageBox.Show($"Successfully loaded {data.Rows.Count} rows.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Error loading initial data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task StartTestAsync()
    {
        if (_connectionSettings == null || _dataset == null)
        {
            MessageBox.Show("Please load initial data first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var enabledSPs = StoredProcedures.Where(sp => sp.IsEnabled).OrderBy(sp => sp.Order).ToList();
        if (!enabledSPs.Any())
        {
            MessageBox.Show("Please enable at least one stored procedure.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsRunning = true;
            CurrentIteration = 0;
            StatusMessage = "Running stress test...";

            _cancellationTokenSource = new CancellationTokenSource();

            var factory = new SqlConnectionFactory(_connectionSettings);
            var spExecutionService = new StoredProcedureExecutionService(factory);
            var metricsService = new MetricsService();
            ResultLoggingService? loggingService = LogToDatabase ? new ResultLoggingService(factory) : null;
            var runnerService = new StressTestRunnerService(factory, spExecutionService, metricsService, loggingService);

            var settings = GetSettings();
            var parameterMappings = GetParameterMappingsFromEvent();

            var progress = new Progress<(int current, int total, TestRunState state)>(p =>
            {
                CurrentIteration = p.current;
                OnPropertyChanged(nameof(ProgressPercentage));
                StatusMessage = $"Running: {p.current}/{p.total} iterations ({p.state})";
            });

            var summary = await runnerService.RunTestAsync(
                settings,
                _dataset,
                parameterMappings,
                progress,
                _cancellationTokenSource.Token);

            StatusMessage = $"Test completed! TPS: {summary.TpsPerIteration:F2}";
            TestCompleted?.Invoke(this, summary);

            MessageBox.Show($"Test completed!\nTotal Iterations: {summary.TotalIterations}\nSuccessful: {summary.SuccessfulIterations}\nFailed: {summary.FailedIterations}\nTPS: {summary.TpsPerIteration:F2}",
                "Test Completed", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Test cancelled.";
            MessageBox.Show("Test was cancelled.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Error running test: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsRunning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void CancelTest()
    {
        _cancellationTokenSource?.Cancel();
    }

    public StressTestSettings GetSettings()
    {
        System.Diagnostics.Debug.WriteLine("=== GetSettings called ===");
        System.Diagnostics.Debug.WriteLine($"  CommandTimeout: {CommandTimeout}");
        System.Diagnostics.Debug.WriteLine($"  MaxDegreeOfParallelism: {MaxDegreeOfParallelism}");
        System.Diagnostics.Debug.WriteLine($"  TotalIterations: {TotalIterations}");
        System.Diagnostics.Debug.WriteLine($"  RecycleDataset: {RecycleDataset}");
        System.Diagnostics.Debug.WriteLine($"  LogToDatabase: {LogToDatabase}");

        var settings = new StressTestSettings
        {
            InitialQuery = InitialQuery,
            CommandTimeout = CommandTimeout,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            TotalIterations = TotalIterations,
            RecycleDataset = RecycleDataset,
            LogToDatabase = LogToDatabase,
            LogTableName = LogTableName
        };

        // Copy SPs to collection
        foreach (var sp in StoredProcedures)
        {
            settings.StoredProcedures.Add(sp);
        }

        // Backward compatibility: populate legacy properties
        var enabledSPs = StoredProcedures.Where(sp => sp.IsEnabled).OrderBy(sp => sp.Order).ToList();
        if (enabledSPs.Count > 0)
            settings.StoredProcedure1 = enabledSPs[0].SqlCommand;
        if (enabledSPs.Count > 1)
            settings.StoredProcedure2 = enabledSPs[1].SqlCommand;

        return settings;
    }

    public void LoadSettings(StressTestSettings settings)
    {
        InitialQuery = settings.InitialQuery;
        CommandTimeout = settings.CommandTimeout;
        MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism;
        TotalIterations = settings.TotalIterations;
        RecycleDataset = settings.RecycleDataset;
        LogToDatabase = settings.LogToDatabase;
        LogTableName = settings.LogTableName;

        // Load from new format if available
        if (settings.StoredProcedures.Any())
        {
            StoredProcedures.Clear();
            foreach (var sp in settings.StoredProcedures)
            {
                StoredProcedures.Add(sp);
            }
        }
        else
        {
            // Load from legacy format
            StoredProcedures.Clear();
            if (!string.IsNullOrEmpty(settings.StoredProcedure1))
            {
                StoredProcedures.Add(new StoredProcedureConfiguration
                {
                    Name = "SP1",
                    SqlCommand = settings.StoredProcedure1,
                    Order = 1,
                    IsEnabled = true
                });
            }
            if (!string.IsNullOrEmpty(settings.StoredProcedure2))
            {
                StoredProcedures.Add(new StoredProcedureConfiguration
                {
                    Name = "SP2",
                    SqlCommand = settings.StoredProcedure2,
                    Order = 2,
                    IsEnabled = true
                });
            }
        }

        // Notify that stored procedures have been loaded
        System.Diagnostics.Debug.WriteLine($"LoadSettings: Loaded {StoredProcedures.Count} stored procedures");
        OnStoredProceduresChanged();
    }

    private List<ParameterMapping> _cachedMappings = new();

    // Property to allow MainViewModel to provide ParameterMappingViewModel reference
    public Func<List<ParameterMapping>>? GetParameterMappingsFunc { get; set; }

    public void CacheMappings(List<ParameterMapping> mappings)
    {
        _cachedMappings = mappings;
    }

    private List<ParameterMapping> GetParameterMappingsFromEvent()
    {
        // Try to get mappings from the func first (new way)
        if (GetParameterMappingsFunc != null)
        {
            return GetParameterMappingsFunc();
        }

        // Fallback to cached mappings (legacy)
        return _cachedMappings;
    }
}
