/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatMemoryStore.cs
    文件功能描述：AdminChatMemoryStore 相关定义与实现


    创建标识：Senparc - 20261103

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 完善聊天记忆的存储、检索与上下文处理

----------------------------------------------------------------*/

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Senparc.Ncf.Core.Exceptions;

namespace Senparc.Areas.Admin.Domain.Services;

public interface IAdminChatMemoryStore
{
    Task<AdminChatMemoryProfile> ReadAsync(int tenantId, int userId, CancellationToken cancellationToken = default);
    Task<AdminChatMemoryProfile> UpdateAsync(int tenantId, int userId, string revision,
        Action<AdminChatMemoryProfile> update, CancellationToken cancellationToken = default);
}

// Retained only as the migration reader for installations using the original private file store.
public sealed class AdminChatMemoryStore : IAdminChatMemoryStore
{
    private readonly string _root;
    private readonly IDataProtectionProvider _protection;

    public AdminChatMemoryStore(IWebHostEnvironment environment, IDataProtectionProvider protection)
    {
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "AdminChatMemory"));
        if (!string.IsNullOrEmpty(environment.WebRootPath)
            && (_root + Path.DirectorySeparatorChar).StartsWith(
                Path.GetFullPath(environment.WebRootPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Memory storage must be outside wwwroot.");
        _protection = protection;
    }

    public Task<AdminChatMemoryProfile> ReadAsync(int tenantId, int userId, CancellationToken cancellationToken = default) =>
        AccessAsync(tenantId, userId, null, null, cancellationToken);

    public Task<AdminChatMemoryProfile> UpdateAsync(int tenantId, int userId, string revision,
        Action<AdminChatMemoryProfile> update, CancellationToken cancellationToken = default) =>
        AccessAsync(tenantId, userId, revision, update, cancellationToken);

    private async Task<AdminChatMemoryProfile> AccessAsync(int tenantId, int userId, string revision,
        Action<AdminChatMemoryProfile> update, CancellationToken cancellationToken)
    {
        if (tenantId < 0 || userId < 0) throw new ArgumentOutOfRangeException(nameof(userId));
        var directory = Path.Combine(_root, tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (update == null && !Directory.Exists(directory)) return new AdminChatMemoryProfile();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, userId.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".protected");
        var protector = _protection.CreateProtector("NCF.AdminChat.Memory.v1", tenantId.ToString(), userId.ToString());
        await using var lease = await AcquireAsync(path + ".lock", cancellationToken);
        if (File.Exists(path) && new FileInfo(path).Length > 512 * 1024)
            throw new NcfExceptionBase("Memory storage exceeds its size limit.");
        var profile = File.Exists(path)
            ? JsonSerializer.Deserialize<AdminChatMemoryProfile>(
                protector.Unprotect(await File.ReadAllBytesAsync(path, cancellationToken)))
                ?? throw new NcfExceptionBase("Invalid Memory storage.")
            : new AdminChatMemoryProfile();
        if (update == null) return profile;
        if (revision == null || profile.Revision != revision)
            throw new NcfExceptionBase("Memory changed. Reload before saving; the newer version was preserved.");
        update(profile);
        if (profile.Sessions.Count > 2000) throw new NcfExceptionBase("Disable Memory in an older chat before enabling more sessions.");
        profile.Revision = Guid.NewGuid().ToString("N");
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var encrypted = protector.Protect(JsonSerializer.SerializeToUtf8Bytes(profile));
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                             4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(encrypted, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return profile;
    }

    private static async Task<FileStream> AcquireAsync(string path, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (attempt < 40)
            {
                await Task.Delay(50, cancellationToken);
            }
        }
    }
}
