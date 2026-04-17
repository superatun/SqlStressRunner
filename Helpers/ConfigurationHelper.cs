using System.IO;
using System.Text.Json;
using SqlStressRunner.Models;

namespace SqlStressRunner.Helpers;

public class ConfigurationHelper
{
    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SqlStressRunner",
        "config.json");

    public static async Task SaveConfigurationAsync(AppConfiguration config)
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
        }
    }

    public static async Task<AppConfiguration?> LoadConfigurationAsync()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
                return null;

            var json = await File.ReadAllTextAsync(ConfigFilePath);
            return JsonSerializer.Deserialize<AppConfiguration>(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
            return null;
        }
    }
}
