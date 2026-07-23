# Grant SQL database permissions to the API managed identity
# This script creates a contained user for the workload UAMI and grants db_datareader + db_datawriter

param(
    [Parameter(Mandatory=$true)]
    [string]$SqlServer,

    [Parameter(Mandatory=$true)]
    [string]$Database,

    [Parameter(Mandatory=$true)]
    [string]$ManagedIdentityName,

    [Parameter(Mandatory=$true)]
    [string]$ManagedIdentityObjectId
)

$ErrorActionPreference = 'Stop'

Write-Host "Granting SQL permissions to managed identity: $ManagedIdentityName (OID: $ManagedIdentityObjectId)"

# Convert object ID to SQL SID (16-byte little-endian)
$oid = [guid]::Parse($ManagedIdentityObjectId)
$sid = [System.BitConverter]::ToString($oid.ToByteArray()).Replace('-', '')

# Build the T-SQL command
$sql = @"
DECLARE @miName SYSNAME = N'$ManagedIdentityName';
DECLARE @miNameQ NVARCHAR(500) = QUOTENAME(@miName);
DECLARE @miOid UNIQUEIDENTIFIER = CAST(N'$ManagedIdentityObjectId' AS UNIQUEIDENTIFIER);
DECLARE @miSid VARBINARY(85) = CAST(@miOid AS VARBINARY(16));
DECLARE @sql NVARCHAR(MAX);

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @miName)
BEGIN
    SET @sql = N'CREATE USER ' + @miNameQ +
               N' WITH SID = ' + CONVERT(NVARCHAR(200), @miSid, 1) +
               N', TYPE = E;';
    EXEC sys.sp_executesql @sql;
    PRINT N'Created contained user ' + @miName + N' with SID=' + CONVERT(NVARCHAR(200), @miSid, 1);
END
ELSE
BEGIN
    PRINT N'Contained user ' + @miName + N' already exists.';
END

SET @sql = N'ALTER ROLE db_datareader ADD MEMBER ' + @miNameQ + N';';
BEGIN TRY
    EXEC sys.sp_executesql @sql;
END TRY
BEGIN CATCH
    PRINT ERROR_MESSAGE();
END CATCH

SET @sql = N'ALTER ROLE db_datawriter ADD MEMBER ' + @miNameQ + N';';
BEGIN TRY
    EXEC sys.sp_executesql @sql;
END TRY
BEGIN CATCH
    PRINT ERROR_MESSAGE();
END CATCH

PRINT N'Granted db_datareader + db_datawriter to ' + @miName;
"@

Write-Host "Executing SQL script..."
Write-Host $sql

# Connect and execute
$connectionString = "Server=$SqlServer;Database=$Database;Authentication=Active Directory Interactive;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()

    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 60
    
    $result = $command.ExecuteNonQuery()
    Write-Host "✅ SQL command executed successfully"
    
    $connection.Close()
} catch {
    Write-Host "❌ Error executing SQL command: $_"
    throw
}
