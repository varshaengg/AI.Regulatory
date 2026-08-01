using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;

namespace AI.Regulatory.API.Data;

/// <summary>
/// Opens a <see cref="SqlConnection"/> to Azure SQL.
/// </summary>
/// <remarks>
/// <para>
/// When the connection string does not embed auth credentials (i.e. no
/// <c>Authentication=Active Directory …</c> keyword), the factory obtains
/// an Azure AD access token via <see cref="DefaultAzureCredential"/> and
/// injects it as <see cref="SqlConnection.AccessToken"/>. This is the
/// recommended pattern for UAMI on App Service: it avoids SqlClient's
/// internal managed-identity code path and relies on Azure.Identity which
/// correctly handles the App Service MSI endpoint and reads
/// <c>AZURE_CLIENT_ID</c> to select the right user-assigned identity.
/// </para>
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
    private const int MaxOpenAttempts = 3;

    // Azure SQL token audience — the same regardless of region or database name.
    private static readonly string[] SqlTokenScopes = ["https://database.windows.net/.default"];

    private readonly string _connectionString;
    private readonly bool _useTokenAuth;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlConnectionFactory> _log;

    public SqlConnectionFactory(IConfiguration config, ILogger<SqlConnectionFactory> log)
    {
        var artaConnectionString = config.GetConnectionString("ArtaSql");
        var sqlConnectionString  = config["Sql:ConnectionString"];

        _connectionString = !string.IsNullOrWhiteSpace(artaConnectionString)
            ? artaConnectionString
            : !string.IsNullOrWhiteSpace(sqlConnectionString)
                ? sqlConnectionString
                : throw new InvalidOperationException(
                    "ConnectionStrings:ArtaSql or Sql:ConnectionString must be configured with a non-empty value.");

        // Use explicit token acquisition (DefaultAzureCredential) when the
        // connection string does NOT already embed an Authentication keyword.
        // This makes UAMI work reliably on App Service without relying on
        // SqlClient's own managed-identity code path (which has known issues
        // when User ID isn't propagated through every SqlConnectionStringBuilder
        // round-trip, and does not read AZURE_CLIENT_ID by itself).
        var csb = new SqlConnectionStringBuilder(_connectionString);
        _useTokenAuth = csb.Authentication == SqlAuthenticationMethod.NotSpecified;

        _credential = new DefaultAzureCredential();
        _log = log;

        var azureClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        _log.LogInformation(
            "SqlConnectionFactory initialised. Server={Server} Database={Database} TokenAuth={UseTokenAuth} AZURE_CLIENT_ID={AzureClientId}",
            csb.DataSource, csb.InitialCatalog,
            _useTokenAuth,
            string.IsNullOrEmpty(azureClientId) ? "(not set — system-assigned or local auth)" : azureClientId);
    }

    public async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        for (var attempt = 1; attempt <= MaxOpenAttempts; attempt++)
        {
            var conn = new SqlConnection(_connectionString);
            try
            {
                if (_useTokenAuth)
                {
                    var tokenResponse = await _credential.GetTokenAsync(
                        new TokenRequestContext(SqlTokenScopes), ct);
                    conn.AccessToken = tokenResponse.Token;
                }

                await conn.OpenAsync(ct);
                return conn;
            }
            catch (SqlException ex) when (attempt < MaxOpenAttempts && IsTransientSqlError(ex))
            {
                await conn.DisposeAsync();
                var delay = TimeSpan.FromSeconds(attempt * 2);
                _log.LogWarning(ex,
                    "Transient SQL open failure (error {ErrorNumber}) on attempt {Attempt}/{MaxAttempts}. Retrying in {DelaySeconds}s.",
                    ex.Number, attempt, MaxOpenAttempts, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
            catch
            {
                await conn.DisposeAsync();
                _log.LogError("Failed to open SQL connection on attempt {Attempt}", attempt);
                throw;
            }
        }

        throw new InvalidOperationException("SQL connection retry policy exhausted unexpectedly.");
    }

    private static bool IsTransientSqlError(SqlException ex) => ex.Number is
        40613 or 40197 or 40501 or 49918 or 49919 or 49920;
}
