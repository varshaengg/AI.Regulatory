CREATE TABLE [dbo].[CtdTemplate]
(
    [Id]          UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CtdTemplate_Id] DEFAULT NEWSEQUENTIALID(),
    [ProjectId]   UNIQUEIDENTIFIER NULL,
    [ModuleId]    NVARCHAR(10)     NOT NULL,
    [FileName]    NVARCHAR(260)    NOT NULL,
    [StoragePath] NVARCHAR(1024)   NOT NULL,
    [Version]     NVARCHAR(50)     NOT NULL,
    [UploadedBy]  NVARCHAR(256)    NOT NULL,
    [UploadedOn]  DATETIME2(3)     NOT NULL CONSTRAINT [DF_CtdTemplate_UploadedOn] DEFAULT SYSUTCDATETIME(),
    [Status]      NVARCHAR(20)     NOT NULL CONSTRAINT [DF_CtdTemplate_Status] DEFAULT N'Active',
    [RowVersion]  ROWVERSION       NOT NULL,
    CONSTRAINT [PK_CtdTemplate] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_CtdTemplate_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_CtdTemplate_ModuleId] CHECK ([ModuleId] IN (N'M1', N'M2', N'M3', N'M4', N'M5')),
    CONSTRAINT [CK_CtdTemplate_FileNamePdf] CHECK (LOWER(RIGHT([FileName], 4)) = N'.pdf')
);
GO

CREATE UNIQUE INDEX [UX_CtdTemplate_Global_Module_Active]
    ON [dbo].[CtdTemplate] ([ModuleId])
    WHERE [ProjectId] IS NULL AND [Status] <> N'Archived';
GO

CREATE UNIQUE INDEX [UX_CtdTemplate_Project_Module_Active]
    ON [dbo].[CtdTemplate] ([ProjectId], [ModuleId])
    WHERE [ProjectId] IS NOT NULL AND [Status] <> N'Archived';
GO
