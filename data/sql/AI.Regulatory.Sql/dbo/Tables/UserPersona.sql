CREATE TABLE [dbo].[UserPersona]
(
    [UserId]     INT           NOT NULL,
    [PersonaId]  INT           NOT NULL,
    [AssignedUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_UserPersona_AssignedUtc] DEFAULT (SYSUTCDATETIME()),
    [AssignedBy] NVARCHAR(200) NULL,
    CONSTRAINT [PK_UserPersona] PRIMARY KEY CLUSTERED ([UserId], [PersonaId]),
    CONSTRAINT [FK_UserPersona_User]    FOREIGN KEY ([UserId])    REFERENCES [dbo].[AppUser]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserPersona_Persona] FOREIGN KEY ([PersonaId]) REFERENCES [dbo].[Persona]([Id]) ON DELETE CASCADE
);
GO
CREATE INDEX [IX_UserPersona_Persona] ON [dbo].[UserPersona] ([PersonaId]);
