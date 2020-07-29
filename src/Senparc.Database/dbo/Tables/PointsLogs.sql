CREATE TABLE [dbo].[PointsLogs] (
    [Id]              INT             IDENTITY (1, 1) NOT NULL,
    [Flag]            BIT             NOT NULL,
    [AddTime]         DATETIME2 (7)   NOT NULL,
    [LastUpdateTime]  DATETIME2 (7)   NOT NULL,
    [AdminRemark]     NVARCHAR (300)  NULL,
    [Remark]          NVARCHAR (300)  NULL,
    [AccountId]       INT             NOT NULL,
    [AccountPayLogId] INT             NULL,
    [Points]          DECIMAL (18, 2) NOT NULL,
    [BeforePoints]    DECIMAL (18, 2) NOT NULL,
    [AfterPoints]     DECIMAL (18, 2) NOT NULL,
    [Description]     NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_PointsLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PointsLogs_AccountPayLogs_AccountPayLogId] FOREIGN KEY ([AccountPayLogId]) REFERENCES [dbo].[AccountPayLogs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PointsLogs_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [dbo].[Accounts] ([Id])
);




GO
CREATE NONCLUSTERED INDEX [IX_PointsLogs_AccountPayLogId]
    ON [dbo].[PointsLogs]([AccountPayLogId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_PointsLogs_AccountId]
    ON [dbo].[PointsLogs]([AccountId] ASC);

