using System.Windows;
using SqlStressRunner.Commands;
using SqlStressRunner.Helpers;
using SqlStressRunner.Infrastructure;
using SqlStressRunner.Models;
using SqlStressRunner.Services;

namespace SqlStressRunner.ViewModels;

public class ConnectionViewModel : ViewModelBase
{
    private string _server = "localhost";
    private string _database = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _useIntegratedSecurity = true;
    private bool _trustServerCertificate = true;
    private int _connectionTimeout = 30;
    private string _statusMessage = string.Empty;
    private string _loggingTableBaseName = "StressTestLog";

    public event EventHandler<DatabaseConnectionSettings>? ConnectionSettingsChanged;

    public ConnectionViewModel()
    {
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        SetupDatabaseCommand = new AsyncRelayCommand(SetupDatabaseAsync);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
    }

    public string Server
    {
        get => _server;
        set => SetProperty(ref _server, value);
    }

    public string Database
    {
        get => _database;
        set => SetProperty(ref _database, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool UseIntegratedSecurity
    {
        get => _useIntegratedSecurity;
        set => SetProperty(ref _useIntegratedSecurity, value);
    }

    public bool TrustServerCertificate
    {
        get => _trustServerCertificate;
        set => SetProperty(ref _trustServerCertificate, value);
    }

    public int ConnectionTimeout
    {
        get => _connectionTimeout;
        set => SetProperty(ref _connectionTimeout, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string LoggingTableBaseName
    {
        get => _loggingTableBaseName;
        set => SetProperty(ref _loggingTableBaseName, value);
    }

    public AsyncRelayCommand TestConnectionCommand { get; }
    public AsyncRelayCommand SetupDatabaseCommand { get; }
    public RelayCommand SaveSettingsCommand { get; }

    private async Task TestConnectionAsync()
    {
        try
        {
            StatusMessage = "Testing connection...";
            var settings = GetSettings();
            var testService = new ConnectionTestService();
            var (success, message) = await testService.TestConnectionAsync(settings);

            StatusMessage = message;

            if (success)
            {
                MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Error testing connection: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SetupDatabaseAsync()
    {
        try
        {
            StatusMessage = "Validating connection for database setup...";
            var settings = GetSettings();
            var testService = new ConnectionTestService();
            var (success, message) = await testService.TestConnectionAsync(settings);

            if (!success)
            {
                StatusMessage = message;
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StatusMessage = "Creating or updating logging schema...";
            var factory = new SqlConnectionFactory(settings);
            var setupService = new DatabaseSetupService(factory);
            var result = await setupService.SetupAsync(LoggingTableBaseName);

            StatusMessage = result.Message;
            MessageBox.Show(result.Message, "Database Setup", MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Error setting up database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveSettings()
    {
        var settings = GetSettings();
        ConnectionSettingsChanged?.Invoke(this, settings);

        // Auto-save connection settings to config file (password excluded via [JsonIgnore])
        _ = SaveConnectionSettingsAsync(settings);

        MessageBox.Show("Connection settings saved!\n\nNote: Password is not saved for security reasons and must be entered each time.", 
            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task SaveConnectionSettingsAsync(DatabaseConnectionSettings settings)
    {
        try
        {
            // Load existing config or create new
            var config = await ConfigurationHelper.LoadConfigurationAsync() ?? new AppConfiguration();

            // Update connection settings (password will be excluded by [JsonIgnore])
            config.ConnectionSettings = settings;

            // Save config
            await ConfigurationHelper.SaveConfigurationAsync(config);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error auto-saving connection settings: {ex.Message}");
        }
    }

    public DatabaseConnectionSettings GetSettings()
    {
        return new DatabaseConnectionSettings
        {
            Server = Server,
            Database = Database,
            Username = Username,
            Password = Password,
            UseIntegratedSecurity = UseIntegratedSecurity,
            TrustServerCertificate = TrustServerCertificate,
            ConnectionTimeout = ConnectionTimeout
        };
    }

    public void LoadSettings(DatabaseConnectionSettings settings)
    {
        Server = settings.Server;
        Database = settings.Database;
        Username = settings.Username;
        Password = settings.Password;
        UseIntegratedSecurity = settings.UseIntegratedSecurity;
        TrustServerCertificate = settings.TrustServerCertificate;
        ConnectionTimeout = settings.ConnectionTimeout;
    }
}
