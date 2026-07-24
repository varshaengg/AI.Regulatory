using Microsoft.Data.SqlClient;

namespace AI.Regulatory.API.Data;

/// <summary>
/// Opens a <see cref="SqlConnection"/> to Azure SQL using the configured
/// connection string auth mode.
/// </summary>
/// <remarks>
/// <para>
/// Connection pooling is handled by SqlClient — we return a fresh
/// <see cref="SqlConnection"/> per call.
/// </para>
/// </remarks>
public interface ISqlConnectionFactory
{
    /// <summary>Opens and returns a SQL connection.</summary>
    Task<SqlConnection> OpenAsync(CancellationToken ct = default);
}

/// <inheritdoc cref="ISqlConnectionFactory" />
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<SqlConnectionFactory> _log;

    public SqlConnectionFactory(IConfiguration config, ILogger<SqlConnectionFactory> log)
    {
        _connectionString =
            config.GetConnectionString("ArtaSql")
            ?? config["Sql:ConnectionString"]
            ?? throw new InvalidOperationException(
                "ConnectionStrings:ArtaSql or Sql:ConnectionString is not configured.");
        _log = log;
    }

    public async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        var conn = new SqlConnection(builder.ConnectionString);

        try
        {
            await conn.OpenAsync(ct);
            return conn;
        }
        catch
        {
            await conn.DisposeAsync();
            _log.LogError("Failed to open SQL connection to {Server}", builder.DataSource);
            throw;
        }
    }
}
