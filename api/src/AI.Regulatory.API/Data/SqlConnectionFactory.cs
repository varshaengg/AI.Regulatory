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
    private const int MaxOpenAttempts = 3;
    private readonly string _connectionString;
    private readonly ILogger<SqlConnectionFactory> _log;

    public SqlConnectionFactory(IConfiguration config, ILogger<SqlConnectionFactory> log)
    {
        var artaConnectionString = config.GetConnectionString("ArtaSql");
        var sqlConnectionString = config["Sql:ConnectionString"];

        _connectionString = !string.IsNullOrWhiteSpace(artaConnectionString)
            ? artaConnectionString
            : !string.IsNullOrWhiteSpace(sqlConnectionString)
                ? sqlConnectionString
                : throw new InvalidOperationException(
                    "ConnectionStrings:ArtaSql or Sql:ConnectionString must be configured with a non-empty value.");
        _log = log;

        // Diagnostic: log the identity that will be used so MSI issues are visible in App Insights.
        var azureClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        var builder = new SqlConnectionStringBuilder(_connectionString);
        _log.LogInformation(
            "SqlConnectionFactory initialised. Server={Server} Database={Database} Auth={Auth} AZURE_CLIENT_ID={AzureClientId}",
            builder.DataSource, builder.InitialCatalog,
            builder.Authentication.ToString(),
            string.IsNullOrEmpty(azureClientId) ? "(not set — system-assigned identity)" : azureClientId);
    }

    public async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);

        for (var attempt = 1; attempt <= MaxOpenAttempts; attempt++)
        {
            var conn = new SqlConnection(builder.ConnectionString);
            try
            {
                await conn.OpenAsync(ct);
                return conn;
            }
            catch (SqlException ex) when (attempt < MaxOpenAttempts && IsTransientSqlError(ex))
            {
                await conn.DisposeAsync();
                var delay = TimeSpan.FromSeconds(attempt * 2);
                _log.LogWarning(ex,
                    "Transient SQL open failure to {Server} (error {ErrorNumber}) on attempt {Attempt}/{MaxAttempts}. Retrying in {DelaySeconds}s.",
                    builder.DataSource,
                    ex.Number,
                    attempt,
                    MaxOpenAttempts,
                    delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
            catch
            {
                await conn.DisposeAsync();
                _log.LogError("Failed to open SQL connection to {Server}", builder.DataSource);
                throw;
            }
        }

        throw new InvalidOperationException("SQL connection retry policy exhausted unexpectedly.");
    }

    private static bool IsTransientSqlError(SqlException ex) => ex.Number is
        // Azure SQL transient conditions, including DB unavailable during resume.
        40613 or 40197 or 40501 or 49918 or 49919 or 49920;
}
