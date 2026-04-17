using System.Text.Json.Serialization;

namespace SqlStressRunner.Models;

public class DatabaseConnectionSettings
{
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    public bool UseIntegratedSecurity { get; set; }
    public bool TrustServerCertificate { get; set; } = true;
    public int ConnectionTimeout { get; set; } = 30;

    public string GetConnectionString()
    {
        if (UseIntegratedSecurity)
        {
            return $"Server={Server};Database={Database};Integrated Security=true;TrustServerCertificate={TrustServerCertificate};Connection Timeout={ConnectionTimeout}";
        }
        else
        {
            return $"Server={Server};Database={Database};User Id={Username};Password={Password};TrustServerCertificate={TrustServerCertificate};Connection Timeout={ConnectionTimeout}";
        }
    }
}
