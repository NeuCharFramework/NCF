/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatMemoryDatabaseStore.cs
    文件功能描述：AdminChatMemoryDatabaseStore 相关定义与实现


    创建标识：Senparc - 20261103

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 完善聊天记忆的存储、检索与上下文处理

----------------------------------------------------------------*/

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Exceptions;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class AdminChatMemoryDatabaseStore : IAdminChatMemoryStore
{
    private readonly DbContext _db;
    private readonly AdminChatMemoryStore _legacy;
    private readonly IDataProtectionProvider _protection;

    public AdminChatMemoryDatabaseStore(DbContext db, AdminChatMemoryStore legacy, IDataProtectionProvider protection)
    {
        _db = db;
        _legacy = legacy;
        _protection = protection;
    }

    private IQueryable<AdminChatMemoryState> Query(int tenantId, int userId)
    {
        if (tenantId < 0 || userId < 0) throw new ArgumentOutOfRangeException(nameof(userId));
        return _db.Set<AdminChatMemoryState>().Where(row => row.TenantId == tenantId && row.UserId == userId);
    }

    private IDataProtector Protector(int tenantId, int userId) =>
        _protection.CreateProtector("NCF.AdminChat.Memory.Database.v1", tenantId.ToString(), userId.ToString());

    public async Task<AdminChatMemoryProfile> ReadAsync(int tenantId, int userId, CancellationToken cancellationToken = default)
    {
        var row = await Query(tenantId, userId).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (row != null)
            return JsonSerializer.Deserialize<AdminChatMemoryProfile>(Protector(tenantId, userId).Unprotect(row.Payload))
                ?? throw new NcfExceptionBase("Invalid Memory data.");
        var profile = await _legacy.ReadAsync(tenantId, userId, cancellationToken);
        if (profile.Revision.Length == 0) return profile;
        // A database row, including an empty/deleted profile, always wins over a legacy file.
        await InsertAsync(tenantId, userId, profile, cancellationToken);
        return await ReadAsync(tenantId, userId, cancellationToken);
    }

    public async Task<AdminChatMemoryProfile> UpdateAsync(int tenantId, int userId, string revision,
        Action<AdminChatMemoryProfile> update, CancellationToken cancellationToken = default)
    {
        var profile = await ReadAsync(tenantId, userId, cancellationToken);
        if (revision == null || revision != profile.Revision) throw Conflict();
        update(profile);
        if (profile.Sessions.Count + profile.SessionModes.Count > 2000)
            throw new NcfExceptionBase("Memory session limit exceeded.");
        profile.Revision = Guid.NewGuid().ToString("N");
        var payload = Protect(tenantId, userId, profile);
        var count = await Query(tenantId, userId).Where(row => row.Revision == revision)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Revision, profile.Revision)
                .SetProperty(row => row.Payload, payload).SetProperty(row => row.LastUpdateTime, DateTime.Now), cancellationToken);
        if (count == 0 && (revision.Length != 0 || !await InsertAsync(tenantId, userId, profile, cancellationToken)))
            throw Conflict();
        return profile;
    }

    private byte[] Protect(int tenantId, int userId, AdminChatMemoryProfile profile)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(profile);
        if (json.Length > 1024 * 1024) throw new NcfExceptionBase("Memory data exceeds 1 MiB.");
        return Protector(tenantId, userId).Protect(json);
    }

    private async Task<bool> InsertAsync(int tenantId, int userId, AdminChatMemoryProfile profile, CancellationToken ct)
    {
        var row = new AdminChatMemoryState
        {
            TenantId = tenantId, UserId = userId, Revision = profile.Revision, Payload = Protect(tenantId, userId, profile)
        };
        _db.Add(row);
        try { await _db.SaveChangesAsync(ct); return true; }
        catch (DbUpdateException)
        {
            _db.Entry(row).State = EntityState.Detached;
            if (await Query(tenantId, userId).AnyAsync(ct)) return false;
            throw;
        }
        finally { _db.Entry(row).State = EntityState.Detached; }
    }

    private static NcfExceptionBase Conflict() =>
        new("Memory changed. Reload before saving; the newer version was preserved.");
}
