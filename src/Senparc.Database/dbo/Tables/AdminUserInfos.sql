CREATE TABLE [dbo].[AdminUserInfos] (
    [Id]             INT            IDENTITY (1, 1) NOT NULL,
    [Flag]           BIT            NOT NULL,
    [AddTime]        DATETIME2 (7)  NOT NULL,
    [LastUpdateTime] DATETIME2 (7)  NOT NULL,
    [AdminRemark]    NVARCHAR (300) NULL,
    [Remark]         NVARCHAR (300) NULL,
    [UserName]       NVARCHAR (MAX) NULL,
    [Password]       NVARCHAR (MAX) NULL,
    [PasswordSalt]   NVARCHAR (MAX) NULL,
    [RealName]       NVARCHAR (MAX) NULL,
    [Phone]          NVARCHAR (MAX) NULL,
    [Note]           NVARCHAR (MAX) NULL,
    [ThisLoginTime]  DATETIME2 (7)  NOT NULL,
    [ThisLoginIp]    NVARCHAR (MAX) NULL,
    [LastLoginTime]  DATETIME2 (7)  NOT NULL,
    [LastLoginIp]    NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_AdminUserInfos] PRIMARY KEY CLUSTERED ([Id] ASC)
);





