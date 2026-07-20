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
-- Bootstrap: contained user for API managed identity (optional)
-- ---------------------------------------------------------------------------
IF N'$(ApiManagedIdentityName)' <> N''
BEGIN
    DECLARE @miName SYSNAME       = N'$(ApiManagedIdentityName)';
    DECLARE @miNameQ NVARCHAR(500) = QUOTENAME(@miName);
    DECLARE @sql NVARCHAR(MAX);

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @miName)
    BEGIN
        SET @sql = N'CREATE USER ' + @miNameQ + N' FROM EXTERNAL PROVIDER;';
        EXEC sys.sp_executesql @sql;
    END

    SET @sql = N'ALTER ROLE db_datareader ADD MEMBER ' + @miNameQ + N';';
    EXEC sys.sp_executesql @sql;
    SET @sql = N'ALTER ROLE db_datawriter ADD MEMBER ' + @miNameQ + N';';
    EXEC sys.sp_executesql @sql;

    PRINT N'Granted db_datareader + db_datawriter to ' + @miName;
END
ELSE
BEGIN
    PRINT 'ApiManagedIdentityName not supplied - skipped MI contained-user bootstrap.';
END
GO
