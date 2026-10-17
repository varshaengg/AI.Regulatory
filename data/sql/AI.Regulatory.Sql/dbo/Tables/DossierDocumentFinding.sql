CREATE TABLE [dbo].[DossierDocumentFinding]
(
    [Id]                UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_DossierDocumentFinding_Id] DEFAULT NEWSEQUENTIALID(),
    [DossierRunId]      UNIQUEIDENTIFIER NOT NULL,
    [DossierDocumentId] UNIQUEIDENTIFIER NOT NULL,
    [RuleKey]           NVARCHAR(50)     NOT NULL,   -- key within the guideline frozen in the run snapshot
    [Verdict]           NVARCHAR(10)     NOT NULL,
    [Evidence]          NVARCHAR(500)    NULL,
    [CreatedUtc]        DATETIME2(3)     NOT NULL CONSTRAINT [DF_DossierDocumentFinding_Created] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_DossierDocumentFinding] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_DossierDocumentFinding_Run] FOREIGN KEY ([DossierRunId]) REFERENCES [dbo].[DossierRun]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DossierDocumentFinding_Document] FOREIGN KEY ([DossierDocumentId]) REFERENCES [dbo].[DossierDocument]([Id]),
    CONSTRAINT [CK_DossierDocumentFinding_Verdict] CHECK ([Verdict] IN (N'pass', N'flag', N'unclear'))
);
GO

CREATE INDEX [IX_DossierDocumentFinding_Run] ON [dbo].[DossierDocumentFinding] ([DossierRunId], [DossierDocumentId]);
GO
