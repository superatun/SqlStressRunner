using Microsoft.Data.SqlClient;
using SqlStressRunner.Models;

namespace SqlStressRunner.Services;

public class SqlConnectionFactory
{
    private readonly DatabaseConnectionSettings _settings;

    public SqlConnectionFactory(DatabaseConnectionSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_settings.GetConnectionString());
    }
}
