/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DatabaseUpgradeCoordinator.cs
    文件功能描述：不依赖业务页面的数据库检查与升级协调器

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.35.0 新增数据库升级维护流程与多平台下载入口

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.Web.Infrastructure.Database;

public sealed class DatabaseUpgradeCoordinator
{
    private const string XncfModuleManagerName = "Senparc.Xncf.XncfModuleManager";
    private const string SystemManagerName = "Senparc.Xncf.SystemManager";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DatabaseRuntimeStateStore _stateStore;

    public DatabaseUpgradeCoordinator(
        IServiceScopeFactory scopeFactory,
        DatabaseRuntimeStateStore stateStore)
    {
        _scopeFactory = scopeFactory;
        _stateStore = stateStore;
    }

    public async Task<DatabaseRuntimeState> InspectAsync(CancellationToken cancellationToken = default)
    {
        var installFinished = SiteConfig.CheckInstallFinishedFileExisted();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            HashSet<string> installedUids = null;

            if (!installFinished)
            {
                // 首次安装没有完成标记。只读取 XncfModules.Uid；如果表不存在，外层会把状态判定为
                // Uninitialized。这样不会把一套全新数据库中的所有初始迁移误认为“升级”。
                installedUids = await LoadInstalledModuleUidsAsync(scope.ServiceProvider, cancellationToken);
                if (installedUids.Count == 0)
                {
                    return SetState(DatabaseRuntimeState.Uninitialized("未发现已安装模块，系统需要初始化。"));
                }
            }

            // 必须先检查两个基础模块。XncfModules 自身的架构落后时，不能先用完整实体模型读取模块清单。
            var pendingModules = await InspectPendingMigrationsAsync(
                scope.ServiceProvider,
                SelectCriticalDatabaseModules(),
                cancellationToken);
            if (pendingModules.Count > 0)
            {
                return SetState(DatabaseRuntimeState.UpgradeRequired(
                    $"检测到 {pendingModules.Count} 个基础模块存在待执行的数据库迁移。",
                    pendingModules));
            }

            installedUids ??= await LoadInstalledModuleUidsAsync(scope.ServiceProvider, cancellationToken);
            pendingModules.AddRange(await InspectPendingMigrationsAsync(
                scope.ServiceProvider,
                SelectInstalledDatabaseModules(installedUids),
                cancellationToken));

            if (pendingModules.Count == 0)
            {
                // 迁移历史也可能被手工改动。实际读取一次核心配置实体，确保“迁移已记录但字段缺失”
                // 不会被误报为 Ready；该检查只在启动时执行一次，不进入请求热路径。
                await ValidateCoreSchemaAsync(scope.ServiceProvider, cancellationToken);
            }

            return pendingModules.Count > 0
                ? SetState(DatabaseRuntimeState.UpgradeRequired(
                    $"检测到 {pendingModules.Count} 个模块存在待执行的数据库迁移。",
                    pendingModules))
                : SetState(DatabaseRuntimeState.Ready());
        }
        catch (Exception ex) when (DatabaseInstallState.IsSchemaUpgradeRequired(ex))
        {
            return SetState(DatabaseRuntimeState.UpgradeRequired(
                "数据库存在，但架构落后于当前程序。",
                Array.Empty<DatabaseModuleMigration>(),
                ex));
        }
        catch (Exception ex) when (DatabaseInstallState.IsDatabaseUnavailableForInstallation(ex))
        {
            // 有完成标记时禁止重新开放 Installer，以免覆盖已有数据库。
            return installFinished
                ? SetState(DatabaseRuntimeState.Unavailable(
                    "已安装数据库当前不可用，请检查连接、权限和基础表完整性。",
                    ex))
                : SetState(DatabaseRuntimeState.Uninitialized("数据库尚未初始化。", ex));
        }
        catch (Exception ex)
        {
            return SetState(DatabaseRuntimeState.Unavailable("数据库状态检查失败。", ex));
        }
    }

    public async Task<DatabaseUpgradeResult> UpgradeAsync(CancellationToken cancellationToken = default)
    {
        var before = await InspectAsync(cancellationToken).ConfigureAwait(false);
        if (before.Status == DatabaseRuntimeStatus.Ready)
        {
            return DatabaseUpgradeResult.Success("数据库无需升级。", before, Array.Empty<string>());
        }

        if (before.Status != DatabaseRuntimeStatus.UpgradeRequired || before.Modules.Count == 0)
        {
            return DatabaseUpgradeResult.Failure(
                "没有取得可安全执行的待迁移模块列表；未修改数据库。",
                before,
                Array.Empty<string>());
        }

        var messages = new List<string>();
        try
        {
            await using var fileLock = await AcquireFileLockAsync(cancellationToken).ConfigureAwait(false);
            var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            using (await cache.BeginCacheLockAsync("NCF.DatabaseUpgrade", "Global").ConfigureAwait(false))
            {
                // 取得锁后重新检查，避免另一个实例已经完成升级后仍使用旧的待迁移列表。
                var current = await InspectAsync(cancellationToken).ConfigureAwait(false);
                for (var batch = 0; batch < 32; batch++)
                {
                    if (current.Status == DatabaseRuntimeStatus.Ready)
                    {
                        return DatabaseUpgradeResult.Success("数据库升级完成。", current, messages);
                    }

                    if (current.Status != DatabaseRuntimeStatus.UpgradeRequired || current.Modules.Count == 0)
                    {
                        return DatabaseUpgradeResult.Failure(
                            "没有取得可安全执行的待迁移模块列表；未继续修改数据库。",
                            current,
                            messages);
                    }

                    foreach (var module in OrderModules(current.Modules))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        using var scope = _scopeFactory.CreateScope();
                        var register = XncfRegisterManager.RegisterList
                            .First(item => string.Equals(item.Uid, module.Uid, StringComparison.OrdinalIgnoreCase));
                        var databaseRegister = (IXncfDatabase)register;
                        var dbContext = ResolveDbContext(scope.ServiceProvider, databaseRegister);

                        messages.Add($"开始升级 {module.Name}: {string.Join(", ", module.PendingMigrations)}");
                        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

                        // Migration 成功后再执行模块自己的升级初始化；失败时不会提前写入模块版本。
                        await register.InstallOrUpdateAsync(
                                scope.ServiceProvider,
                                Senparc.Ncf.Core.Enums.InstallOrUpdate.Update)
                            .ConfigureAwait(false);
                        await UpdateStoredModuleVersionAsync(scope.ServiceProvider, register, cancellationToken)
                            .ConfigureAwait(false);
                        messages.Add($"完成升级 {module.Name} -> {module.Version}");
                    }

                    // 基础模块完成后，再次检查才会安全地发现其余已安装模块的迁移。
                    current = await InspectAsync(cancellationToken).ConfigureAwait(false);
                }

                return DatabaseUpgradeResult.Failure(
                    "数据库升级批次超过安全上限，已停止继续执行。",
                    _stateStore.Current,
                    messages);
            }
        }
        catch (Exception ex)
        {
            var failedState = DatabaseRuntimeState.UpgradeRequired(
                "数据库升级执行失败；网站仍保持维护模式。",
                _stateStore.Current.Modules,
                ex);
            SetState(failedState);
            return DatabaseUpgradeResult.Failure("数据库升级失败。", failedState, messages);
        }
    }

    private DatabaseRuntimeState SetState(DatabaseRuntimeState state)
    {
        _stateStore.Set(state);
        return state;
    }

    private static async Task<HashSet<string>> LoadInstalledModuleUidsAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var dataContext = serviceProvider.GetRequiredService<SenparcEntitiesBase>();
        var uids = await dataContext.Set<XncfModule>()
            .AsNoTracking()
            .Where(module => !module.Flag)
            .Select(module => module.Uid)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return uids.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static async Task ValidateCoreSchemaAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var dataContext = serviceProvider.GetRequiredService<SenparcEntitiesBase>();
        _ = await dataContext.Set<SystemConfig>()
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<List<DatabaseModuleMigration>> InspectPendingMigrationsAsync(
        IServiceProvider serviceProvider,
        IEnumerable<DatabaseModuleTarget> targets,
        CancellationToken cancellationToken)
    {
        var pendingModules = new List<DatabaseModuleMigration>();
        foreach (var target in targets)
        {
            var dbContext = ResolveDbContext(serviceProvider, target.DatabaseRegister);
            var pendingMigrations = (await dbContext.Database
                    .GetPendingMigrationsAsync(cancellationToken)
                    .ConfigureAwait(false))
                .ToArray();
            if (pendingMigrations.Length == 0)
            {
                continue;
            }

            pendingModules.Add(new DatabaseModuleMigration(
                target.Register.Uid,
                target.Register.Name,
                target.Register.Version,
                dbContext.GetType(),
                pendingMigrations));
        }

        return pendingModules;
    }

    private static IEnumerable<DatabaseModuleTarget> SelectCriticalDatabaseModules()
    {
        return XncfRegisterManager.RegisterList
            .Where(register => register is IXncfDatabase && IsCriticalModule(register.Name))
            .Select(register => new DatabaseModuleTarget(register, (IXncfDatabase)register))
            .OrderBy(target => GetModulePriority(target.Register.Name));
    }

    private static IEnumerable<DatabaseModuleTarget> SelectInstalledDatabaseModules(
        IReadOnlySet<string> installedUids)
    {
        return XncfRegisterManager.RegisterList
            .Where(register => register is IXncfDatabase)
            .Where(register => !IsCriticalModule(register.Name) && installedUids.Contains(register.Uid))
            .Select(register => new DatabaseModuleTarget(register, (IXncfDatabase)register))
            .OrderBy(target => GetModulePriority(target.Register.Name))
            .ThenBy(target => target.Register.Name, StringComparer.Ordinal);
    }

    private static bool IsCriticalModule(string moduleName) =>
        string.Equals(moduleName, XncfModuleManagerName, StringComparison.Ordinal)
        || string.Equals(moduleName, SystemManagerName, StringComparison.Ordinal);

    private static IEnumerable<DatabaseModuleMigration> OrderModules(
        IEnumerable<DatabaseModuleMigration> modules)
    {
        return modules
            .OrderBy(module => GetModulePriority(module.Name))
            .ThenBy(module => module.Name, StringComparer.Ordinal);
    }

    private static int GetModulePriority(string moduleName) => moduleName switch
    {
        XncfModuleManagerName => 0,
        SystemManagerName => 1,
        _ => 10
    };

    private static DbContext ResolveDbContext(IServiceProvider serviceProvider, IXncfDatabase databaseRegister)
    {
        var dbContextType = databaseRegister.TryGetXncfDatabaseDbContextType;
        return serviceProvider.GetService(dbContextType) as DbContext
            ?? throw new InvalidOperationException($"无法创建数据库上下文：{dbContextType.FullName}");
    }

    private static async Task UpdateStoredModuleVersionAsync(
        IServiceProvider serviceProvider,
        IXncfRegister register,
        CancellationToken cancellationToken)
    {
        var dataContext = serviceProvider.GetRequiredService<SenparcEntitiesBase>();
        var module = await dataContext.Set<XncfModule>()
            .FirstOrDefaultAsync(item => item.Uid == register.Uid, cancellationToken)
            .ConfigureAwait(false);
        if (module == null || string.Equals(module.Version, register.Version, StringComparison.Ordinal))
        {
            return;
        }

        module.UpdateVersion(register.Version, register.MenuName, register.Description);
        await dataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<FileStream> AcquireFileLockAsync(CancellationToken cancellationToken)
    {
        // 锁目录使用系统临时目录，而非发布目录。这样同机的蓝绿发布目录仍会竞争同一把锁，
        // 同时避免只读发布目录导致升级命令无法启动。Redis 缓存锁继续负责跨主机协调。
        var databaseIdentity = $"{SiteConfig.SenparcCoreSetting?.DatabaseType}:{SiteConfig.SenparcCoreSetting?.DatabaseName}";
        var identityHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(databaseIdentity)))[..24];
        var lockDirectory = Path.Combine(Path.GetTempPath(), "ncf-database-upgrade-locks");
        Directory.CreateDirectory(lockDirectory);
        var path = Path.Combine(lockDirectory, $"{identityHash}.lock");
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private sealed record DatabaseModuleTarget(IXncfRegister Register, IXncfDatabase DatabaseRegister);
}

public sealed record DatabaseUpgradeResult(
    bool Succeeded,
    string Message,
    DatabaseRuntimeState State,
    IReadOnlyList<string> Details)
{
    public static DatabaseUpgradeResult Success(
        string message,
        DatabaseRuntimeState state,
        IReadOnlyList<string> details) => new(true, message, state, details);

    public static DatabaseUpgradeResult Failure(
        string message,
        DatabaseRuntimeState state,
        IReadOnlyList<string> details) => new(false, message, state, details);
}
