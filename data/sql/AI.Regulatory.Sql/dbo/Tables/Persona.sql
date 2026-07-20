CREATE TABLE [dbo].[Persona]
(
    [Id]         INT            IDENTITY(1,1) NOT NULL,
    [Code]       NVARCHAR(50)   NOT NULL,
    [Name]       NVARCHAR(100)  NOT NULL,
    [Description] NVARCHAR(400) NULL,
    [IsSystem]   BIT            NOT NULL CONSTRAINT [DF_Persona_IsSystem] DEFAULT (0),
    [CreatedUtc] DATETIME2(3)   NOT NULL CONSTRAINT [DF_Persona_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Persona] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Persona_Code] UNIQUE ([Code])
);
