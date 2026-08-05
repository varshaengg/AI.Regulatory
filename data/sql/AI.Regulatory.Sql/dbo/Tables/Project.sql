CREATE TABLE [dbo].[Project]
(
    [Id]                    UNIQUEIDENTIFIER NOT NULL,
    [TenantId]              UNIQUEIDENTIFIER NOT NULL,
    [Name]                  NVARCHAR(200)    NOT NULL,
    [Status]                TINYINT          NOT NULL CONSTRAINT [DF_Project_Status] DEFAULT (0),
    [Country]               CHAR(2)          NOT NULL,
    [Product]               NVARCHAR(80)     NOT NULL CONSTRAINT [DF_Project_Product] DEFAULT (N''),
    [Procedure]             NVARCHAR(50)     NOT NULL,
    [Applicant]             NVARCHAR(200)    NOT NULL,
    [Description]           NVARCHAR(2000)   NULL,
    [DiscoveryStarted]      BIT              NOT NULL CONSTRAINT [DF_Project_DiscoveryStarted] DEFAULT (0),
    [CtdTemplateVersionId]  UNIQUEIDENTIFIER  NULL,
    [OwnerEmail]            NVARCHAR(320)    NOT NULL CONSTRAINT [DF_Project_OwnerEmail] DEFAULT (N''),
    [OwnerDisplayName]      NVARCHAR(200)    NOT NULL CONSTRAINT [DF_Project_OwnerDisplayName] DEFAULT (N''),
    [ProgressPct]           INT              NOT NULL CONSTRAINT [DF_Project_ProgressPct] DEFAULT (0),
    [CreatedUtc]            DATETIME2(3)     NOT NULL CONSTRAINT [DF_Project_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedUtc]            DATETIME2(3)     NOT NULL CONSTRAINT [DF_Project_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             NVARCHAR(200)    NOT NULL,
    CONSTRAINT [PK_Project] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Project_Tenant_Name] UNIQUE ([TenantId], [Name])
);
