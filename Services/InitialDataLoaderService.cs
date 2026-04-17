using System.Data;
using Microsoft.Data.SqlClient;
using SqlStressRunner.Models;

namespace SqlStressRunner.Services;

public class InitialDataLoaderService
{
    private readonly SqlConnectionFactory _connectionFactory;

    public InitialDataLoaderService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<DataTable> LoadDataAsync(string query, int commandTimeout)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(query, connection)
        {
            CommandTimeout = commandTimeout,
            CommandType = CommandType.Text
        };

        using var adapter = new SqlDataAdapter(command);
        var dataTable = new DataTable();
        await Task.Run(() => adapter.Fill(dataTable));

        return dataTable;
    }

    public List<string> GetColumnNames(DataTable dataTable)
    {
        return dataTable.Columns.Cast<DataColumn>()
            .Select(c => c.ColumnName)
            .ToList();
    }
}
