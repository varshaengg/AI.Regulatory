CREATE TABLE [dbo].[DossierRun]
(
    [Id]                   UNIQUEIDENTIFIER NOT NULL,
    [ProjectId]           UNIQUEIDENTIFIER NOT NULL,
    [CtdTemplateVersionId] UNIQUEIDENTIFIER NOT NULL,
    [Status]               TINYINT          NOT NULL,
    [StartedUtc]           DATETIME2(3)     NOT NULL,
    [CompletedUtc]         DATETIME2(3)     NULL,
    [StartedBy]            NVARCHAR(200)    NOT NULL,
    [ApprovedBy]           NVARCHAR(200)    NULL,
    [PackageBlobPath]      NVARCHAR(500)    NULL,
    [ManifestBlobPath]     NVARCHAR(500)    NULL,
    [AssembledPdfPath]     NVARCHAR(500)    NULL,
    [AssembledDocxPath]    NVARCHAR(500)    NULL,
    [GapRunId]             UNIQUEIDENTIFIER NULL,
    [ErrorPayload]         NVARCHAR(MAX)    NULL,
    CONSTRAINT [PK_DossierRun] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_DossierRun_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_DossierRun_Project_Started] ON [dbo].[DossierRun] ([ProjectId], [StartedUtc] DESC);
GO
