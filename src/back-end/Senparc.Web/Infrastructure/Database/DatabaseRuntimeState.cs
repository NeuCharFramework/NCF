/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DatabaseRuntimeState.cs
    文件功能描述：保存一次性数据库启动检查结果

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.35.0 新增数据库升级维护流程与多平台下载入口

----------------------------------------------------------------*/

namespace Senparc.Web.Infrastructure.Database;

public enum DatabaseRuntimeStatus
{
    Uninitialized,
    UpgradeRequired,
    Ready,
    Unavailable
}

public sealed record DatabaseModuleMigration(
    string Uid,
    string Name,
    string Version,
    Type DbContextType,
    IReadOnlyList<string> PendingMigrations);

public sealed record DatabaseRuntimeState(
    DatabaseRuntimeStatus Status,
    string Message,
    IReadOnlyList<DatabaseModuleMigration> Modules,
    Exception Exception = null)
{
    public static DatabaseRuntimeState Uninitialized(string message, Exception exception = null) =>
        new(DatabaseRuntimeStatus.Uninitialized, message, Array.Empty<DatabaseModuleMigration>(), exception);

    public static DatabaseRuntimeState Ready() =>
        new(DatabaseRuntimeStatus.Ready, "数据库架构已就绪。", Array.Empty<DatabaseModuleMigration>());

    public static DatabaseRuntimeState UpgradeRequired(
        string message,
        IReadOnlyList<DatabaseModuleMigration> modules,
        Exception exception = null) =>
        new(DatabaseRuntimeStatus.UpgradeRequired, message, modules, exception);

    public static DatabaseRuntimeState Unavailable(string message, Exception exception) =>
        new(DatabaseRuntimeStatus.Unavailable, message, Array.Empty<DatabaseModuleMigration>(), exception);
}

/// <summary>
/// 启动时检查一次并复用结果，避免每个 HTTP 请求重复访问迁移历史表。
/// </summary>
public sealed class DatabaseRuntimeStateStore
{
    private DatabaseRuntimeState _current = DatabaseRuntimeState.Uninitialized("数据库状态尚未检查。");

    public DatabaseRuntimeState Current => Volatile.Read(ref _current);

    public void Set(DatabaseRuntimeState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Volatile.Write(ref _current, state);
    }
}
