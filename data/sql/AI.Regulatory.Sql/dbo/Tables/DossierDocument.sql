CREATE TABLE [dbo].[DossierDocument]
(
    [Id]            UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_DossierDocument_Id] DEFAULT NEWID(),
    [DossierRunId]  UNIQUEIDENTIFIER NOT NULL,
    [ModuleId]      NVARCHAR(10)     NOT NULL,
    [SourceLabel]   NVARCHAR(200)    NOT NULL,
    [SourceType]    NVARCHAR(40)     NOT NULL,
    [BlobPath]      NVARCHAR(1000)   NOT NULL,   -- account/container/blob-name
    [FileName]      NVARCHAR(400)    NOT NULL,
    [ContentType]   NVARCHAR(200)    NULL,
    [SizeBytes]     BIGINT           NOT NULL,
    [ETag]          NVARCHAR(100)    NULL,
    [LastModifiedUtc] DATETIME2(3)   NULL,
    [Status]        NVARCHAR(20)     NOT NULL CONSTRAINT [DF_DossierDocument_Status] DEFAULT N'Discovered',
    [DiscoveredUtc] DATETIME2(3)     NOT NULL CONSTRAINT [DF_DossierDocument_Discovered] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_DossierDocument] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_DossierDocument_DossierRun] FOREIGN KEY ([DossierRunId]) REFERENCES [dbo].[DossierRun]([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_DossierDocument_Run_Module] ON [dbo].[DossierDocument] ([DossierRunId], [ModuleId]);
GO
