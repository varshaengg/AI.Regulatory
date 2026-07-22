-- ============================================================================
-- SQL Script: Make Sopan an Admin User
-- ============================================================================
-- This script adds Sopan (sopan@varshassive.onmicrosoft.com) as an Admin
-- to the deployed AI Regulatory Assistant database.
-- 
-- Object ID: 1a403da5-4458-420a-adaf-6ff802800cd8
-- Email:    sopan@varshassive.onmicrosoft.com
-- Name:     Sopan
-- ============================================================================

SET NOCOUNT ON;

-- Step 1: Insert into AppUser table (or update if already exists)
MERGE [dbo].[AppUser] AS tgt
USING (
    SELECT 
        CAST('1a403da5-4458-420a-adaf-6ff802800cd8' AS UNIQUEIDENTIFIER) AS [AadObjectId],
        'Sopan' AS [DisplayName],
        'sopan@varshassive.onmicrosoft.com' AS [Email],
        'sopan@varshassive.onmicrosoft.com' AS [Upn],
        1 AS [IsActive]
) AS src
    ON tgt.[AadObjectId] = src.[AadObjectId]
WHEN MATCHED THEN
    UPDATE SET 
        [DisplayName] = src.[DisplayName],
        [Email] = src.[Email],
        [Upn] = src.[Upn],
        [IsActive] = src.[IsActive],
        [UpdatedUtc] = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([AadObjectId], [DisplayName], [Email], [Upn], [IsActive], [CreatedUtc], [UpdatedUtc])
    VALUES (src.[AadObjectId], src.[DisplayName], src.[Email], src.[Upn], src.[IsActive], SYSUTCDATETIME(), SYSUTCDATETIME());

-- Step 2: Get the AppUser ID we just created/updated
DECLARE @UserId INT;
SELECT @UserId = [Id] FROM [dbo].[AppUser] 
WHERE [AadObjectId] = CAST('1a403da5-4458-420a-adaf-6ff802800cd8' AS UNIQUEIDENTIFIER);

IF @UserId IS NULL
BEGIN
    RAISERROR('Failed to insert/find AppUser for Sopan', 16, 1);
END
ELSE
BEGIN
    -- Step 3: Get the Admin Persona ID
    DECLARE @AdminPersonaId INT;
    SELECT @AdminPersonaId = [Id] FROM [dbo].[Persona] WHERE [Code] = 'Admin';
    
    IF @AdminPersonaId IS NULL
    BEGIN
        RAISERROR('Admin persona not found in database', 16, 1);
    END
    ELSE
    BEGIN
        -- Step 4: Check if the user already has Admin persona assigned
        IF NOT EXISTS (
            SELECT 1 FROM [dbo].[UserPersona] 
            WHERE [UserId] = @UserId AND [PersonaId] = @AdminPersonaId
        )
        BEGIN
            -- Insert the Admin persona assignment
            INSERT INTO [dbo].[UserPersona] ([UserId], [PersonaId], [AssignedUtc], [AssignedBy])
            VALUES (@UserId, @AdminPersonaId, SYSUTCDATETIME(), 'system-bootstrap');
            
            PRINT 'SUCCESS: Sopan has been assigned the Admin persona';
        END
        ELSE
        BEGIN
            PRINT 'INFO: Sopan already has the Admin persona assigned';
        END
    END
END

-- Verification query
PRINT '';
PRINT '=== VERIFICATION ===';
SELECT 
    u.[Id],
    u.[AadObjectId],
    u.[DisplayName],
    u.[Email],
    p.[Code] AS [Persona],
    u.[CreatedUtc],
    u.[UpdatedUtc]
FROM [dbo].[AppUser] u
LEFT JOIN [dbo].[UserPersona] up ON u.[Id] = up.[UserId]
LEFT JOIN [dbo].[Persona] p ON up.[PersonaId] = p.[Id]
WHERE u.[AadObjectId] = CAST('1a403da5-4458-420a-adaf-6ff802800cd8' AS UNIQUEIDENTIFIER);
