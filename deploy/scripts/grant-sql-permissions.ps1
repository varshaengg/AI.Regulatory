# Grant SQL database permissions to the API managed identity
# This script creates a contained user for the workload UAMI and grants db_datareader + db_datawriter

param(
    [Parameter(Mandatory=$true)]
    [string]$SqlServer,

    [Parameter(Mandatory=$true)]
    [string]$Database,

    [Parameter(Mandatory=$true)]
    [string]$ManagedIdentityName,

    # ObjectId is kept as a parameter for documentation / audit purposes but is
    # no longer used in the SID calculation — CREATE USER … FROM EXTERNAL PROVIDER
    # lets SQL Server resolve the SID directly from Azure AD, avoiding the
    # endian-mismatch bug that occurs when deriving the SID from Guid.ToByteArray().
    [Parameter(Mandatory=$false)]
    [string]$ManagedIdentityObjectId
)

$ErrorActionPreference = 'Stop'

Write-Host "Granting SQL permissions to managed identity: $ManagedIdentityName"

$sql = @"
DECLARE @miName SYSNAME = N'$ManagedIdentityName';
DECLARE @miNameQ NVARCHAR(500) = QUOTENAME(@miName);
DECLARE @sql NVARCHAR(MAX);

-- Drop any existing user that may have been created with a manually-computed
-- SID (which suffers from a .NET Guid.ToByteArray() endian-mismatch against
-- the UUID bytes stored by Azure SQL).  FROM EXTERNAL PROVIDER re-resolves
-- the SID correctly by querying Azure AD.
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @miName)
BEGIN
    SET @sql = N'DROP USER ' + @miNameQ;
    EXEC sys.sp_executesql @sql;
    PRINT N'Dropped existing user ' + @miName + N' (will recreate via FROM EXTERNAL PROVIDER).';
END

SET @sql = N'CREATE USER ' + @miNameQ + N' FROM EXTERNAL PROVIDER;';
EXEC sys.sp_executesql @sql;
PRINT N'Created contained user ' + @miName + N' (SID resolved by Azure AD).';

SET @sql = N'ALTER ROLE db_datareader ADD MEMBER ' + @miNameQ + N';';
BEGIN TRY EXEC sys.sp_executesql @sql; END TRY BEGIN CATCH PRINT ERROR_MESSAGE(); END CATCH

SET @sql = N'ALTER ROLE db_datawriter ADD MEMBER ' + @miNameQ + N';';
BEGIN TRY EXEC sys.sp_executesql @sql; END TRY BEGIN CATCH PRINT ERROR_MESSAGE(); END CATCH

PRINT N'Granted db_datareader + db_datawriter to ' + @miName;
"@

Write-Host "Executing SQL script..."
Write-Host $sql

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
