CREATE TABLE [dbo].[ProjectSource]
(
    [Id]           INT               IDENTITY(1,1) NOT NULL,
    [ProjectId]    UNIQUEIDENTIFIER  NOT NULL,
    [ModuleId]     NVARCHAR(10)      NOT NULL,
    [Label]        NVARCHAR(200)     NOT NULL,
    [Path]         NVARCHAR(500)     NOT NULL,
    [Type]         NVARCHAR(40)      NOT NULL,
    [SyncedAt]     DATETIME2(3)      NOT NULL,
    [Status]       NVARCHAR(20)      NOT NULL,
    CONSTRAINT [PK_ProjectSource] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_ProjectSource_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([Id]) ON DELETE CASCADE
);
