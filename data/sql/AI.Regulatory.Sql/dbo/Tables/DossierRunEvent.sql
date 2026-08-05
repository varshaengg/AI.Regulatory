CREATE TABLE [dbo].[DossierRunEvent]
(
    [Id]            BIGINT           IDENTITY(1,1) NOT NULL,
    [DossierRunId]  UNIQUEIDENTIFIER NOT NULL,
    [OccurredUtc]   DATETIME2(3)     NOT NULL,
    [Stage]         NVARCHAR(40)    NOT NULL,
    [Severity]      NVARCHAR(20)    NOT NULL,
    [Message]       NVARCHAR(1000)   NOT NULL,
    [Payload]       NVARCHAR(MAX)    NULL,
    CONSTRAINT [PK_DossierRunEvent] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_DossierRunEvent_DossierRun] FOREIGN KEY ([DossierRunId]) REFERENCES [dbo].[DossierRun]([Id]) ON DELETE CASCADE
);
GO
