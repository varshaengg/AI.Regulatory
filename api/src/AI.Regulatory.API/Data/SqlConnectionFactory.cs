using Microsoft.Data.SqlClient;

namespace AI.Regulatory.API.Data;

/// <summary>
/// Opens a <see cref="SqlConnection"/> to Azure SQL using the configured
/// connection string auth mode.
/// </summary>
/// <remarks>
/// <para>
/// In App Service the token comes from the workload's User-Assigned Managed
/// Identity — pinned via the <c>AZURE_CLIENT_ID</c> environment variable
/// (wired by the Bicep template alongside <c>userAssignedIdentityResourceId</c>).
/// </para>
/// <para>
/// Tokens are cached inside <see cref="DefaultAzureCredential"/>; a new one is
/// fetched only when the previous expires. Connection pooling is handled by
/// SqlClient — we return a fresh <see cref="SqlConnection"/> per call.
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
        _connectionString = config["Sql:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Sql:ConnectionString is not configured. Set it as an app-setting or in appsettings.json.");
        _log = log;
    }

    public async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        var conn = new SqlConnection(_connectionString);

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
