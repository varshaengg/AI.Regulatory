CREATE TABLE [dbo].[ModuleGuideline]
(
    [Id]          UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ModuleGuideline_Id] DEFAULT NEWSEQUENTIALID(),
    [Country]     NVARCHAR(10)     NOT NULL CONSTRAINT [DF_ModuleGuideline_Country] DEFAULT N'Global',
    [ModuleId]    NVARCHAR(10)     NOT NULL,
    [Version]     NVARCHAR(50)     NOT NULL,
    [FileName]    NVARCHAR(260)    NOT NULL,
    [StoragePath] NVARCHAR(1024)   NOT NULL,
    [UploadedBy]  NVARCHAR(256)    NOT NULL,
    [UploadedOn]  DATETIME2(3)     NOT NULL CONSTRAINT [DF_ModuleGuideline_UploadedOn] DEFAULT SYSUTCDATETIME(),
    [Status]      NVARCHAR(20)     NOT NULL CONSTRAINT [DF_ModuleGuideline_Status] DEFAULT N'Active',
    [IsPublished] BIT              NOT NULL CONSTRAINT [DF_ModuleGuideline_IsPublished] DEFAULT 0,
    [ExtractionNote] NVARCHAR(500) NULL,
    [RowVersion]  ROWVERSION       NOT NULL,
    CONSTRAINT [PK_ModuleGuideline] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_ModuleGuideline_ModuleId] CHECK ([ModuleId] IN (N'M1', N'M2', N'M3', N'M4', N'M5'))
);
GO

-- One published guideline per country and module.
CREATE UNIQUE INDEX [UX_ModuleGuideline_Country_Module_Published]
    ON [dbo].[ModuleGuideline] ([Country], [ModuleId])
    WHERE [Status] <> N'Archived' AND [IsPublished] = 1;
GO
