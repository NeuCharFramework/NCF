CREATE TABLE [dbo].[SystemConfigs] (
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [Flag]              BIT            NOT NULL,
    [AddTime]           DATETIME2 (7)  NOT NULL,
    [LastUpdateTime]    DATETIME2 (7)  NOT NULL,
    [AdminRemark]       NVARCHAR (300) NULL,
    [Remark]            NVARCHAR (300) NULL,
    [SystemName]        NVARCHAR (100) NOT NULL,
    [MchId]             VARCHAR (100)  NULL,
    [MchKey]            VARCHAR (300)  NULL,
    [TenPayAppId]       VARCHAR (100)  NULL,
    [HideModuleManager] BIT            NULL,
    CONSTRAINT [PK_SystemConfigs] PRIMARY KEY CLUSTERED ([Id] ASC)
);









