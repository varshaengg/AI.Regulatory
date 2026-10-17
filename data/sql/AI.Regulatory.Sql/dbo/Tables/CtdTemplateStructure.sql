CREATE TABLE [dbo].[CtdTemplateStructure]
(
    [TemplateId] UNIQUEIDENTIFIER NOT NULL,
    [Status]     NVARCHAR(20)     NOT NULL CONSTRAINT [DF_CtdTemplateStructure_Status] DEFAULT N'Draft',
    [Note]       NVARCHAR(500)    NULL,
    [UpdatedBy]  NVARCHAR(256)    NOT NULL,
    [UpdatedOn]  DATETIME2(3)     NOT NULL CONSTRAINT [DF_CtdTemplateStructure_UpdatedOn] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_CtdTemplateStructure] PRIMARY KEY CLUSTERED ([TemplateId]),
    CONSTRAINT [FK_CtdTemplateStructure_Template] FOREIGN KEY ([TemplateId]) REFERENCES [dbo].[CtdTemplate]([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_CtdTemplateStructure_Status] CHECK ([Status] IN (N'Draft', N'Approved'))
);
GO

CREATE TABLE [dbo].[CtdTemplateNode]
(
    [Id]          UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_CtdTemplateNode_Id] DEFAULT NEWSEQUENTIALID(),
    [TemplateId]  UNIQUEIDENTIFIER NOT NULL,
    [NodeKey]     NVARCHAR(50)     NOT NULL,
    [ParentKey]   NVARCHAR(50)     NULL,
    [Title]       NVARCHAR(300)    NOT NULL,
    [Guidance]    NVARCHAR(2000)   NULL,
    [IsMandatory] BIT              NOT NULL CONSTRAINT [DF_CtdTemplateNode_Mandatory] DEFAULT 1,
    [SortOrder]   INT              NOT NULL CONSTRAINT [DF_CtdTemplateNode_Sort] DEFAULT 0,
    CONSTRAINT [PK_CtdTemplateNode] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_CtdTemplateNode_Template] FOREIGN KEY ([TemplateId]) REFERENCES [dbo].[CtdTemplate]([Id]) ON DELETE CASCADE,
    CONSTRAINT [UX_CtdTemplateNode_Key] UNIQUE ([TemplateId], [NodeKey])
);
GO
