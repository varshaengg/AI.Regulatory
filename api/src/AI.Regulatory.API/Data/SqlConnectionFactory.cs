using Microsoft.Data.SqlClient;

namespace AI.Regulatory.API.Data;

/// <summary>
/// Opens a <see cref="SqlConnection"/> to Azure SQL using the configured
/// connection string auth mode.
/// </summary>
/// <remarks>
/// <para>
/// For user-assigned managed identity, the UAMI client ID must be supplied via
/// <c>Sql:ManagedIdentityClientId</c> so SqlClient can select the correct
/// identity. Connection pooling is handled by SqlClient — we return a fresh
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
    private readonly string? _managedIdentityClientId;
    private readonly ILogger<SqlConnectionFactory> _log;

    public SqlConnectionFactory(IConfiguration config, ILogger<SqlConnectionFactory> log)
    {
        _connectionString = config["Sql:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Sql:ConnectionString is not configured. Set it as an app-setting or in appsettings.json.");
        _managedIdentityClientId = config["Sql:ManagedIdentityClientId"];
        _log = log;
    }

    public async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        if (builder.Authentication == SqlAuthenticationMethod.ActiveDirectoryManagedIdentity &&
            string.IsNullOrWhiteSpace(builder.UserID) &&
            string.IsNullOrWhiteSpace(_managedIdentityClientId))
        {
            throw new InvalidOperationException(
                "Sql:ManagedIdentityClientId is required when using Active Directory Managed Identity with a user-assigned identity.");
        }

        if (builder.Authentication == SqlAuthenticationMethod.ActiveDirectoryManagedIdentity &&
            string.IsNullOrWhiteSpace(builder.UserID) &&
            !string.IsNullOrWhiteSpace(_managedIdentityClientId))
        {
            builder.UserID = _managedIdentityClientId;
        }

        var conn = new SqlConnection(_connectionString);
        conn.ConnectionString = builder.ConnectionString;

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
