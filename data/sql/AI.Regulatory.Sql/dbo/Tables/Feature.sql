CREATE TABLE [dbo].[Feature]
(
    [Id]         INT            IDENTITY(1,1) NOT NULL,
    [Code]       NVARCHAR(80)   NOT NULL,
    [Name]       NVARCHAR(200)  NOT NULL,
    [Category]   NVARCHAR(80)   NULL,
    [SortOrder]  INT            NOT NULL CONSTRAINT [DF_Feature_SortOrder] DEFAULT (0),
    [IsActive]   BIT            NOT NULL CONSTRAINT [DF_Feature_IsActive] DEFAULT (1),
    CONSTRAINT [PK_Feature] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Feature_Code] UNIQUE ([Code])
);
