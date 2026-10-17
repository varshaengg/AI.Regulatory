CREATE TABLE [dbo].[GuidelineRule]
(
    [Id]          UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_GuidelineRule_Id] DEFAULT NEWSEQUENTIALID(),
    [GuidelineId] UNIQUEIDENTIFIER NOT NULL,
    [RuleKey]     NVARCHAR(50)     NOT NULL,
    [Text]        NVARCHAR(2000)   NOT NULL,
    [Severity]    NVARCHAR(10)     NOT NULL CONSTRAINT [DF_GuidelineRule_Severity] DEFAULT N'Medium',
    [NodeRef]     NVARCHAR(100)    NULL,
    [SortOrder]   INT              NOT NULL CONSTRAINT [DF_GuidelineRule_Sort] DEFAULT 0,
    CONSTRAINT [PK_GuidelineRule] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_GuidelineRule_Guideline] FOREIGN KEY ([GuidelineId]) REFERENCES [dbo].[ModuleGuideline]([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_GuidelineRule_Severity] CHECK ([Severity] IN (N'Low', N'Medium', N'High'))
);
GO

CREATE INDEX [IX_GuidelineRule_Guideline] ON [dbo].[GuidelineRule] ([GuidelineId], [SortOrder]);
GO
