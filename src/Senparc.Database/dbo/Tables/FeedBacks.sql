CREATE TABLE [dbo].[FeedBacks] (
    [Id]             INT            IDENTITY (1, 1) NOT NULL,
    [Flag]           BIT            NOT NULL,
    [AddTime]        DATETIME2 (7)  NOT NULL,
    [LastUpdateTime] DATETIME2 (7)  NOT NULL,
    [AdminRemark]    NVARCHAR (300) NULL,
    [Remark]         NVARCHAR (300) NULL,
    [AccountId]      INT            NOT NULL,
    [Content]        NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_FeedBacks] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_FeedBacks_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [dbo].[Accounts] ([Id]) ON DELETE CASCADE
);






GO
CREATE NONCLUSTERED INDEX [IX_FeedBacks_AccountId]
    ON [dbo].[FeedBacks]([AccountId] ASC);

