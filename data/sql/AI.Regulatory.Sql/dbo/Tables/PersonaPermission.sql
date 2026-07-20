CREATE TABLE [dbo].[PersonaPermission]
(
    [PersonaId]    INT NOT NULL,
    [FeatureId]    INT NOT NULL,
    [PermissionId] INT NOT NULL,
    CONSTRAINT [PK_PersonaPermission] PRIMARY KEY CLUSTERED ([PersonaId], [FeatureId], [PermissionId]),
    CONSTRAINT [FK_PersonaPermission_Persona]    FOREIGN KEY ([PersonaId])    REFERENCES [dbo].[Persona]([Id])    ON DELETE CASCADE,
    CONSTRAINT [FK_PersonaPermission_Feature]    FOREIGN KEY ([FeatureId])    REFERENCES [dbo].[Feature]([Id])    ON DELETE CASCADE,
    CONSTRAINT [FK_PersonaPermission_Permission] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permission]([Id]) ON DELETE CASCADE
);
GO
CREATE INDEX [IX_PersonaPermission_Feature]    ON [dbo].[PersonaPermission] ([FeatureId]);
GO
CREATE INDEX [IX_PersonaPermission_Permission] ON [dbo].[PersonaPermission] ([PermissionId]);
