CREATE TABLE [dbo].[AppUser]
(
    [Id]           INT               IDENTITY(1,1) NOT NULL,
    [AadObjectId]  UNIQUEIDENTIFIER  NOT NULL,
    [DisplayName]  NVARCHAR(200)     NOT NULL,
    [Email]        NVARCHAR(320)     NULL,
    [Upn]          NVARCHAR(320)     NULL,
    [IsActive]     BIT               NOT NULL CONSTRAINT [DF_AppUser_IsActive] DEFAULT (1),
    [CreatedUtc]   DATETIME2(3)      NOT NULL CONSTRAINT [DF_AppUser_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedUtc]   DATETIME2(3)      NOT NULL CONSTRAINT [DF_AppUser_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AppUser] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_AppUser_AadObjectId] UNIQUE ([AadObjectId])
);
