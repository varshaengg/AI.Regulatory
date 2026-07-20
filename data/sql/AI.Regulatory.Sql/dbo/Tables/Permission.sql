CREATE TABLE [dbo].[Permission]
(
    [Id]         INT            IDENTITY(1,1) NOT NULL,
    [Code]       NVARCHAR(50)   NOT NULL,
    [Name]       NVARCHAR(100)  NOT NULL,
    [SortOrder]  INT            NOT NULL CONSTRAINT [DF_Permission_SortOrder] DEFAULT (0),
    CONSTRAINT [PK_Permission] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Permission_Code] UNIQUE ([Code])
);
