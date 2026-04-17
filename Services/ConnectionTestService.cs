using Microsoft.Data.SqlClient;
using SqlStressRunner.Models;

namespace SqlStressRunner.Services;

public class ConnectionTestService
{
    public async Task<(bool success, string message)> TestConnectionAsync(DatabaseConnectionSettings settings)
    {
        try
        {
            var connectionString = settings.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return (true, "Connection successful!");
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
    }
}
