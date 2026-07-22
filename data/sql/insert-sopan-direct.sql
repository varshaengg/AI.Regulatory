-- ============================================================================
-- DIRECT SQL: Insert Sopan as Admin (No DACPAC Required)
-- ============================================================================
-- Run this directly in Azure Portal Query Editor or SSMS
-- This is simpler and faster than waiting for the pipeline

SET NOCOUNT ON;

-- Step 1: Check if Sopan already exists
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[AppUser] 
    WHERE [AadObjectId] = CAST('1a403da5-4458-420a-adaf-6ff802800cd8' AS UNIQUEIDENTIFIER)
)
BEGIN
    -- Insert Sopan into AppUser
    INSERT INTO [dbo].[AppUser] ([AadObjectId], [DisplayName], [Email], [Upn], [IsActive], [CreatedUtc], [UpdatedUtc])
    VALUES 
    (
        CAST('1a403da5-4458-420a-adaf-6ff802800cd8' AS UNIQUEIDENTIFIER),
        N'Sopan',
        N'sopan@varshassive.onmicrosoft.com',
        N'sopan@varshassive.onmicrosoft.com',
        1,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );
    
    PRINT 'ℹ️  Inserted Sopan into AppUser table';
END
ELSE
BEGIN
    PRINT 'ℹ️  Sopan already exists in AppUser table';
END

-- Step 2: Get the AppUser ID for Sopan
DECLARE @UserId INT;
SELECT @UserId = [Id] FROM [dbo].[AppUser] 
WHERE [AadObjectId] = CAST('1a403da5-4458-420a-adaf-6ff802800cd8' AS UNIQUEIDENTIFIER);

IF @UserId IS NULL
BEGIN
    PRINT '❌ ERROR: Could not find Sopan in AppUser table';
END
ELSE
BEGIN
    PRINT '✅ Found Sopan: AppUser.Id = ' + CAST(@UserId AS NVARCHAR(10));
    
    -- Step 3: Get Admin Persona ID
    DECLARE @AdminPersonaId INT;
    SELECT @AdminPersonaId = [Id] FROM [dbo].[Persona] WHERE [Code] = N'Admin';
    
    IF @AdminPersonaId IS NULL
    BEGIN
        PRINT '❌ ERROR: Admin persona not found';
    END
    ELSE
    BEGIN
        PRINT '✅ Found Admin persona: Persona.Id = ' + CAST(@AdminPersonaId AS NVARCHAR(10));
        
        -- Step 4: Check if Sopan already has Admin persona
        IF EXISTS (
            SELECT 1 FROM [dbo].[UserPersona] 
            WHERE [UserId] = @UserId AND [PersonaId] = @AdminPersonaId
        )
        BEGIN
            PRINT '⚠️  Sopan already has Admin persona assigned';
        END
        ELSE
        BEGIN
            -- Step 5: Assign Admin persona to Sopan
            INSERT INTO [dbo].[UserPersona] ([UserId], [PersonaId], [AssignedUtc], [AssignedBy])
            VALUES (@UserId, @AdminPersonaId, SYSUTCDATETIME(), N'manual-fix');
            
            PRINT '✅ Assigned Admin persona to Sopan';
        END
    END
END

-- Step 6: Verify the result
PRINT '';
PRINT '================== VERIFICATION ==================';
SELECT 
    u.[Id] AS [UserId],
    u.[AadObjectId] AS [OID],
    u.[DisplayName] AS [Name],
    u.[Email],
    p.[Code] AS [Persona],
    u.[IsActive],
    u.[CreatedUtc]
FROM [dbo].[AppUser] u
LEFT JOIN [dbo].[UserPersona] up ON u.[Id] = up.[UserId]
LEFT JOIN [dbo].[Persona] p ON up.[PersonaId] = p.[Id]
WHERE u.[AadObjectId] = CAST('1a403da5-4458-420a-adaf-6ff802800cd8' AS UNIQUEIDENTIFIER);
