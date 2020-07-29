CREATE TABLE [dbo].[AccountPayLogs] (
    [Id]             INT             IDENTITY (1, 1) NOT NULL,
    [Flag]           BIT             NOT NULL,
    [AddTime]        DATETIME2 (7)   NOT NULL,
    [LastUpdateTime] DATETIME2 (7)   NOT NULL,
    [AdminRemark]    NVARCHAR (300)  NULL,
    [Remark]         NVARCHAR (300)  NULL,
    [AccountId]      INT             NOT NULL,
    [OrderNumber]    VARCHAR (100)   NOT NULL,
    [TotalPrice]     MONEY           NOT NULL,
    [PayMoney]       MONEY           NOT NULL,
    [UsedPoints]     DECIMAL (18, 2) NULL,
    [CompleteTime]   DATETIME        NOT NULL,
    [AddIp]          VARCHAR (50)    NULL,
    [GetPoints]      MONEY           NOT NULL,
    [Status]         TINYINT         NOT NULL,
    [Description]    VARCHAR (250)   NOT NULL,
    [Type]           TINYINT         NULL,
    [TradeNumber]    VARCHAR (150)   NULL,
    [PrepayId]       VARCHAR (100)   NULL,
    [PayType]        INT             NOT NULL,
    [OrderType]      INT             NOT NULL,
    [PayParam]       NVARCHAR (MAX)  NULL,
    [Price]          MONEY           NOT NULL,
    [Fee]            MONEY           NOT NULL,
    CONSTRAINT [PK_AccountPayLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AccountPayLogs_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [dbo].[Accounts] ([Id])
);




GO
CREATE NONCLUSTERED INDEX [IX_AccountPayLogs_AccountId]
    ON [dbo].[AccountPayLogs]([AccountId] ASC);

