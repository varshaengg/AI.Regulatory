/*
    Post-deployment script — idempotent seed for reference data.
    Runs after every dacpac publish. Uses MERGE so re-runs don't duplicate.

    SQLCMD variables (optional):
        $(ApiManagedIdentityName) — display name of the API app service system-assigned MI.
                                    If provided, a contained user is created + granted
                                    db_datareader + db_datawriter.

    Note: because SQL server is AAD-only, a contained user must be added for the API MI
    to connect. That grant is bootstrap-only — it does not survive DB drops.
*/

SET NOCOUNT ON;

-- ---------------------------------------------------------------------------
-- Permission
-- ---------------------------------------------------------------------------
MERGE [dbo].[Permission] AS tgt
USING (VALUES
    ('Read',   'Read',   10),
    ('Write',  'Write',  20),
    ('Review', 'Review', 30),
    ('Admin',  'Admin',  40)
) AS src ([Code], [Name], [SortOrder])
    ON tgt.[Code] = src.[Code]
WHEN MATCHED THEN
    UPDATE SET [Name] = src.[Name], [SortOrder] = src.[SortOrder]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Code], [Name], [SortOrder]) VALUES (src.[Code], src.[Name], src.[SortOrder]);

-- ---------------------------------------------------------------------------
-- Persona
-- ---------------------------------------------------------------------------
MERGE [dbo].[Persona] AS tgt
USING (VALUES
    ('Admin',      'Administrator', 'Full system administration',   1),
    ('RaLead',     'RA Lead',       'Regulatory affairs lead',      1),
    ('RaAuthor',   'RA Author',     'Regulatory affairs author',    1),
    ('RaReviewer', 'RA Reviewer',   'Regulatory affairs reviewer',  1)
) AS src ([Code], [Name], [Description], [IsSystem])
    ON tgt.[Code] = src.[Code]
WHEN MATCHED THEN
    UPDATE SET [Name] = src.[Name], [Description] = src.[Description], [IsSystem] = src.[IsSystem]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Code], [Name], [Description], [IsSystem])
    VALUES (src.[Code], src.[Name], src.[Description], src.[IsSystem]);

-- ---------------------------------------------------------------------------
-- Feature
-- ---------------------------------------------------------------------------
MERGE [dbo].[Feature] AS tgt
USING (VALUES
    ('UserManagement',    'User Management',    'Administration',  10),
    ('DossierManagement', 'Dossier Management', 'Regulatory',      20),
    ('Templates',         'Templates',          'Regulatory',      30),
    ('Assignments',       'Assignments',        'Regulatory',      40),
    ('Reviews',           'Reviews',            'Regulatory',      50),
    ('Notifications',     'Notifications',      'Platform',        60)
) AS src ([Code], [Name], [Category], [SortOrder])
    ON tgt.[Code] = src.[Code]
WHEN MATCHED THEN
    UPDATE SET [Name] = src.[Name], [Category] = src.[Category], [SortOrder] = src.[SortOrder]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Code], [Name], [Category], [SortOrder])
    VALUES (src.[Code], src.[Name], src.[Category], src.[SortOrder]);

-- ---------------------------------------------------------------------------
-- PersonaPermission default matrix
--   Admin       -> every feature: Admin (implies all)
--   RaLead      -> Dossier/Templates/Assignments: Read+Write, Reviews: Read
--   RaAuthor    -> Dossier/Templates/Assignments: Read+Write
--   RaReviewer  -> Dossier: Read+Review, Reviews: Read+Write+Review, others: Read
-- ---------------------------------------------------------------------------
;WITH matrix (PersonaCode, FeatureCode, PermissionCode) AS (
    SELECT * FROM (VALUES
        -- Admin: full access to everything
        ('Admin',      'UserManagement',    'Admin'),
        ('Admin',      'DossierManagement', 'Admin'),
        ('Admin',      'Templates',         'Admin'),
        ('Admin',      'Assignments',       'Admin'),
        ('Admin',      'Reviews',           'Admin'),
        ('Admin',      'Notifications',     'Admin'),
        -- RA Lead
        ('RaLead',     'DossierManagement', 'Read'),
        ('RaLead',     'DossierManagement', 'Write'),
        ('RaLead',     'Templates',         'Read'),
        ('RaLead',     'Templates',         'Write'),
        ('RaLead',     'Assignments',       'Read'),
        ('RaLead',     'Assignments',       'Write'),
        ('RaLead',     'Reviews',           'Read'),
        ('RaLead',     'Notifications',     'Read'),
        -- RA Author
        ('RaAuthor',   'DossierManagement', 'Read'),
        ('RaAuthor',   'DossierManagement', 'Write'),
        ('RaAuthor',   'Templates',         'Read'),
        ('RaAuthor',   'Assignments',       'Read'),
        ('RaAuthor',   'Assignments',       'Write'),
        ('RaAuthor',   'Notifications',     'Read'),
        -- RA Reviewer
        ('RaReviewer', 'DossierManagement', 'Read'),
        ('RaReviewer', 'DossierManagement', 'Review'),
        ('RaReviewer', 'Templates',         'Read'),
        ('RaReviewer', 'Assignments',       'Read'),
        ('RaReviewer', 'Reviews',           'Read'),
        ('RaReviewer', 'Reviews',           'Write'),
        ('RaReviewer', 'Reviews',           'Review'),
        ('RaReviewer', 'Notifications',     'Read')
    ) v (PersonaCode, FeatureCode, PermissionCode)
)
MERGE [dbo].[PersonaPermission] AS tgt
USING (
    SELECT p.[Id] AS PersonaId, f.[Id] AS FeatureId, pm.[Id] AS PermissionId
    FROM matrix m
    JOIN [dbo].[Persona]    p  ON p.[Code]  = m.PersonaCode
    JOIN [dbo].[Feature]    f  ON f.[Code]  = m.FeatureCode
    JOIN [dbo].[Permission] pm ON pm.[Code] = m.PermissionCode
) AS src
    ON  tgt.[PersonaId]    = src.PersonaId
    AND tgt.[FeatureId]    = src.FeatureId
    AND tgt.[PermissionId] = src.PermissionId
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([PersonaId], [FeatureId], [PermissionId])
    VALUES (src.PersonaId, src.FeatureId, src.PermissionId);

-- ---------------------------------------------------------------------------
-- AppUser seed: pre-configure admin users
-- Idempotent: uses MERGE so re-runs don't duplicate
-- ---------------------------------------------------------------------------
MERGE [dbo].[AppUser] AS tgt
USING (VALUES
    -- (AadObjectId, DisplayName, Email, Upn, IsActive)
    (CAST('1a403da5-4458-420a-adaf-6ff802800cd8' AS UNIQUEIDENTIFIER), N'Sopan', N'sopan@varshassive.onmicrosoft.com', N'sopan@varshassive.onmicrosoft.com', 1)
) AS src (AadObjectId, DisplayName, Email, Upn, IsActive)
    ON tgt.[AadObjectId] = src.[AadObjectId]
WHEN MATCHED THEN
    UPDATE SET [DisplayName] = src.[DisplayName], [Email] = src.[Email], [Upn] = src.[Upn], [IsActive] = src.[IsActive], [UpdatedUtc] = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([AadObjectId], [DisplayName], [Email], [Upn], [IsActive], [CreatedUtc], [UpdatedUtc])
    VALUES (src.[AadObjectId], src.[DisplayName], src.[Email], src.[Upn], src.[IsActive], SYSUTCDATETIME(), SYSUTCDATETIME());

-- Assign Admin persona to Sopan
INSERT INTO [dbo].[UserPersona] ([UserId], [PersonaId], [AssignedUtc], [AssignedBy])
SELECT u.[Id], p.[Id], SYSUTCDATETIME(), N'system-seed'
FROM [dbo].[AppUser] u, [dbo].[Persona] p
WHERE u.[AadObjectId] = CAST('1a403da5-4458-420a-adaf-6ff802800cd8' AS UNIQUEIDENTIFIER)
  AND p.[Code] = N'Admin'
  AND NOT EXISTS (
    SELECT 1 FROM [dbo].[UserPersona] up
    WHERE up.[UserId] = u.[Id] AND up.[PersonaId] = p.[Id]
  );

PRINT 'Seeded AppUser: Sopan (1a403da5-4458-420a-adaf-6ff802800cd8) with Admin persona';

-- ---------------------------------------------------------------------------
-- Project lifecycle seed (single-tenant demo data)
-- ---------------------------------------------------------------------------
MERGE [dbo].[Project] AS tgt
USING (VALUES
    (CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER), CAST('00000000-0000-0000-0000-000000000000' AS UNIQUEIDENTIFIER), N'PX-102 · DE · Initial', 1, N'DE', N'PX-102', N'Initial',  N'Contoso Pharma', N'PX-102 Germany initial submission', 1, NULL, N'marcus.l@contoso.com', N'Marcus L.', 55, DATEADD(DAY,-42,SYSUTCDATETIME()), DATEADD(DAY,-1,SYSUTCDATETIME()), N'marcus.l@contoso.com'),
    (CAST('22222222-2222-2222-2222-222222222222' AS UNIQUEIDENTIFIER), CAST('00000000-0000-0000-0000-000000000000' AS UNIQUEIDENTIFIER), N'PX-102 · FR · Initial', 1, N'FR', N'PX-102', N'Initial',  N'Contoso Pharma', N'PX-102 France initial submission', 1, NULL, N'marcus.l@contoso.com', N'Marcus L.', 82, DATEADD(DAY,-30,SYSUTCDATETIME()), DATEADD(DAY,-6,SYSUTCDATETIME()), N'marcus.l@contoso.com'),
    (CAST('33333333-3333-3333-3333-333333333333' AS UNIQUEIDENTIFIER), CAST('00000000-0000-0000-0000-000000000000' AS UNIQUEIDENTIFIER), N'EL-201 · IT · Renewal', 1, N'IT', N'EL-201', N'Renewal', N'Acme Pharma',   N'EL-201 Italy renewal',         1, NULL, N'aisha.k@contoso.com',  N'Aisha K.',   35, DATEADD(DAY,-28,SYSUTCDATETIME()), DATEADD(DAY,-4,SYSUTCDATETIME()), N'aisha.k@contoso.com')
) AS src ([Id], [TenantId], [Name], [Status], [Country], [Product], [Procedure], [Applicant], [Description], [DiscoveryStarted], [CtdTemplateVersionId], [OwnerEmail], [OwnerDisplayName], [ProgressPct], [CreatedUtc], [UpdatedUtc], [CreatedBy])
    ON tgt.[Id] = src.[Id]
WHEN MATCHED THEN
    UPDATE SET [TenantId] = src.[TenantId], [Name] = src.[Name], [Status] = src.[Status], [Country] = src.[Country],
               [Product] = src.[Product], [Procedure] = src.[Procedure], [Applicant] = src.[Applicant],
               [Description] = src.[Description], [DiscoveryStarted] = src.[DiscoveryStarted],
               [CtdTemplateVersionId] = src.[CtdTemplateVersionId], [OwnerEmail] = src.[OwnerEmail],
               [OwnerDisplayName] = src.[OwnerDisplayName], [ProgressPct] = src.[ProgressPct],
               [CreatedUtc] = src.[CreatedUtc], [UpdatedUtc] = src.[UpdatedUtc], [CreatedBy] = src.[CreatedBy]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id], [TenantId], [Name], [Status], [Country], [Product], [Procedure], [Applicant], [Description],
            [DiscoveryStarted], [CtdTemplateVersionId], [OwnerEmail], [OwnerDisplayName], [ProgressPct],
            [CreatedUtc], [UpdatedUtc], [CreatedBy])
    VALUES (src.[Id], src.[TenantId], src.[Name], src.[Status], src.[Country], src.[Product], src.[Procedure], src.[Applicant], src.[Description],
            src.[DiscoveryStarted], src.[CtdTemplateVersionId], src.[OwnerEmail], src.[OwnerDisplayName], src.[ProgressPct],
            src.[CreatedUtc], src.[UpdatedUtc], src.[CreatedBy]);

SET IDENTITY_INSERT [dbo].[ProjectSource] ON;
MERGE [dbo].[ProjectSource] AS tgt
USING (VALUES
    (1, CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER), N'M1', N'Azure Blob primary', N'contosopharma/px102/m1', N'Azure Blob', DATEADD(DAY,-1,SYSUTCDATETIME()), N'ok'),
    (2, CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER), N'M2', N'Azure Blob primary', N'contosopharma/px102/m2', N'Azure Blob', DATEADD(DAY,-1,SYSUTCDATETIME()), N'ok'),
    (3, CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER), N'M3', N'Analytical reports',  N'px102-sharepoint/quality/analytical', N'SharePoint', DATEADD(DAY,-1,SYSUTCDATETIME()), N'ok'),
    (4, CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER), N'M5', N'Clinical trial data', N'contosopharma/px102/m5/ctr', N'Azure Blob', DATEADD(DAY,-2,SYSUTCDATETIME()), N'ok')
) AS src ([Id], [ProjectId], [ModuleId], [Label], [Path], [Type], [SyncedAt], [Status])
    ON tgt.[Id] = src.[Id]
WHEN MATCHED THEN
    UPDATE SET [ProjectId] = src.[ProjectId], [ModuleId] = src.[ModuleId], [Label] = src.[Label], [Path] = src.[Path],
               [Type] = src.[Type], [SyncedAt] = src.[SyncedAt], [Status] = src.[Status]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id], [ProjectId], [ModuleId], [Label], [Path], [Type], [SyncedAt], [Status])
    VALUES (src.[Id], src.[ProjectId], src.[ModuleId], src.[Label], src.[Path], src.[Type], src.[SyncedAt], src.[Status]);
SET IDENTITY_INSERT [dbo].[ProjectSource] OFF;

-- ---------------------------------------------------------------------------
-- Bootstrap: contained user for API managed identity (optional)
--
-- Requires BOTH sqlcmd vars to be set:
--   $(ApiManagedIdentityName)     — the SQL user name (usually the app name)
--   $(ApiManagedIdentityObjectId) — the MI's AAD object id (guid)
--
-- Uses the SID form (CREATE USER ... WITH SID = ..., TYPE = E) which does NOT
-- require the SQL server to have its own AAD MI + Directory Readers role.
-- The SID is the raw 16-byte little-endian representation of the object id.
-- ---------------------------------------------------------------------------
IF N'$(ApiManagedIdentityName)' <> N'' AND N'$(ApiManagedIdentityObjectId)' <> N''
BEGIN
    DECLARE @miName SYSNAME        = N'$(ApiManagedIdentityName)';
    DECLARE @miNameQ NVARCHAR(500) = QUOTENAME(@miName);
    DECLARE @miOid   UNIQUEIDENTIFIER = CAST(N'$(ApiManagedIdentityObjectId)' AS UNIQUEIDENTIFIER);
    DECLARE @miSid   VARBINARY(85)    = CAST(@miOid AS VARBINARY(16));
    DECLARE @sql     NVARCHAR(MAX);

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @miName)
    BEGIN
        SET @sql = N'CREATE USER ' + @miNameQ +
                   N' WITH SID = ' + CONVERT(NVARCHAR(200), @miSid, 1) +
                   N', TYPE = E;';
        EXEC sys.sp_executesql @sql;
        PRINT N'Created contained user ' + @miName + N' with SID=' + CONVERT(NVARCHAR(200), @miSid, 1);
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_role_members drm
        JOIN sys.database_principals r ON r.principal_id = drm.role_principal_id
        JOIN sys.database_principals m ON m.principal_id = drm.member_principal_id
        WHERE r.[name] = N'db_datareader'
          AND m.[name] = @miName
    )
    BEGIN
        SET @sql = N'ALTER ROLE db_datareader ADD MEMBER ' + @miNameQ + N';';
        EXEC sys.sp_executesql @sql;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_role_members drm
        JOIN sys.database_principals r ON r.principal_id = drm.role_principal_id
        JOIN sys.database_principals m ON m.principal_id = drm.member_principal_id
        WHERE r.[name] = N'db_datawriter'
          AND m.[name] = @miName
    )
    BEGIN
        SET @sql = N'ALTER ROLE db_datawriter ADD MEMBER ' + @miNameQ + N';';
        EXEC sys.sp_executesql @sql;
    END

    PRINT N'Granted db_datareader + db_datawriter to ' + @miName;
END
ELSE
BEGIN
    PRINT 'ApiManagedIdentityName / ObjectId not both supplied - skipped MI contained-user bootstrap.';
END
GO
