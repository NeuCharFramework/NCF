/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceAssetShareStore.cs
    文件功能描述：WorkspaceAssetShareStore 相关定义与实现


    创建标识：Senparc - 20261024

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record WorkspaceAssetShare(
    string Id, string Token, int OwnerId, int SessionId, string WorkspaceId,
    string Path, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, string Html);

public sealed class WorkspaceAssetShareStore : IDisposable
{
    private static readonly Regex ShareId = new(@"^[1-9][0-9]{0,9}-[a-f0-9]{32}$", RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));
    private readonly string _root;
    private readonly IDataProtector _protector;
    private readonly ILogger<WorkspaceAssetShareStore> _logger;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly PartitionedRateLimiter<string> _publicLimiter = PartitionedRateLimiter.Create<string, string>(
        key => RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
        }));

    public WorkspaceAssetShareStore(
        IWebHostEnvironment environment, IDataProtectionProvider protection,
        ILogger<WorkspaceAssetShareStore> logger, TimeProvider timeProvider = null)
    {
        _root = System.IO.Path.Combine(environment.ContentRootPath, "App_Data", "WorkspaceAssetShares");
        if (!string.IsNullOrEmpty(environment.WebRootPath)
            && (string.Equals(System.IO.Path.GetFullPath(_root),
                    System.IO.Path.GetFullPath(environment.WebRootPath).TrimEnd(System.IO.Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase)
                || System.IO.Path.GetFullPath(_root).StartsWith(
                    System.IO.Path.GetFullPath(environment.WebRootPath).TrimEnd(System.IO.Path.DirectorySeparatorChar)
                    + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("分享存储不能位于 wwwroot 中。");
        _protector = protection.CreateProtector("NCF.AdminChat.WorkspaceAssetShare.v1");
        _logger = logger;
        _time = timeProvider ?? TimeProvider.System;
    }

    public bool TryAcquirePublicRequest(string remoteAddress)
    {
        using var lease = _publicLimiter.AttemptAcquire(remoteAddress);
        return lease.IsAcquired;
    }

    public async Task<WorkspaceAssetShare> CreateAsync(
        int ownerId, int sessionId, string workspaceId, string path, string html,
        int expiresInHours, CancellationToken cancellationToken = default)
    {
        if (ownerId <= 0 || sessionId <= 0 || string.IsNullOrWhiteSpace(workspaceId))
            throw new ArgumentException("分享必须属于有效的管理员和会话。");
        if (expiresInHours is not (1 or 24 or 168))
            throw new ArgumentException("分享有效期只能是 1 小时、24 小时或 7 天。");
        WorkspaceAssetPreviewBuilder.NormalizePath(path);
        if (Encoding.UTF8.GetByteCount(html) > WorkspaceAssetPreviewBuilder.MaxHtmlBytes)
            throw new ArgumentException("分享快照过大。");
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = GetOwnerDirectory(ownerId);
            Directory.CreateDirectory(directory);
            var shares = await ListCoreAsync(ownerId, cancellationToken);
            if (shares.Count >= 10) throw new InvalidOperationException("每位管理员最多保留 10 个有效分享，请先撤销旧分享。");
            var now = _time.GetUtcNow();
            var share = new WorkspaceAssetShare(
                $"{ownerId}-{Guid.NewGuid():N}", WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)),
                ownerId, sessionId, workspaceId, path, now, now.AddHours(expiresInHours), html);
            var bytes = _protector.Protect(JsonSerializer.SerializeToUtf8Bytes(share));
            var filePath = GetFilePath(share.Id, share.Token);
            var temporaryPath = filePath + ".tmp";
            try
            {
                await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken);
                File.Move(temporaryPath, filePath);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            _logger.LogInformation("Workspace HTML snapshot shared: Owner={OwnerId}, Session={SessionId}, Share={ShareId}, Expires={ExpiresAt}",
                ownerId, sessionId, share.Id, share.ExpiresAt);
            return share;
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<WorkspaceAssetShare>> ListAsync(
        int ownerId, int sessionId, string workspaceId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await ListCoreAsync(ownerId, cancellationToken))
                .Where(share => share.SessionId == sessionId && share.WorkspaceId == workspaceId)
                .Select(share => share with { Html = "" })
                .OrderByDescending(share => share.CreatedAt).ToArray();
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> RevokeAsync(
        int ownerId, int sessionId, string workspaceId, string shareId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var share = (await ListCoreAsync(ownerId, cancellationToken))
                .FirstOrDefault(item => item.Id == shareId && item.SessionId == sessionId && item.WorkspaceId == workspaceId);
            if (share == null) return false;
            File.Delete(GetFilePath(share.Id, share.Token));
            _logger.LogInformation("Workspace HTML snapshot revoked: Owner={OwnerId}, Share={ShareId}", ownerId, shareId);
            return true;
        }
        finally { _gate.Release(); }
    }

    public async Task<WorkspaceAssetShare?> ReadPublicAsync(
        string id, string token, CancellationToken cancellationToken = default)
    {
        if (id == null || !ShareId.IsMatch(id) || token == null || token.Length != 43
            || !token.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
            return null;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = GetFilePath(id, token);
            if (!File.Exists(path)) return null;
            var share = await ReadCoreAsync(path, cancellationToken);
            if (share.ExpiresAt <= _time.GetUtcNow())
            {
                File.Delete(path);
                return null;
            }
            return share.Id == id && CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(share.Token), Encoding.ASCII.GetBytes(token)) ? share : null;
        }
        finally { _gate.Release(); }
    }

    private async Task<List<WorkspaceAssetShare>> ListCoreAsync(int ownerId, CancellationToken cancellationToken)
    {
        var result = new List<WorkspaceAssetShare>();
        var directory = GetOwnerDirectory(ownerId);
        if (!Directory.Exists(directory)) return result;
        foreach (var path in Directory.EnumerateFiles(directory, "*.protected"))
        {
            var share = await ReadCoreAsync(path, cancellationToken);
            if (share.ExpiresAt <= _time.GetUtcNow())
                File.Delete(path);
            else if (share.OwnerId == ownerId)
                result.Add(share);
        }
        return result;
    }

    private async Task<WorkspaceAssetShare> ReadCoreAsync(string path, CancellationToken cancellationToken)
    {
        if (new FileInfo(path).Length > WorkspaceAssetPreviewBuilder.MaxHtmlBytes * 6 + 4096)
            throw new IOException("分享文件超过存储限制。");
        try
        {
            var protectedBytes = await File.ReadAllBytesAsync(path, cancellationToken);
            return JsonSerializer.Deserialize<WorkspaceAssetShare>(_protector.Unprotect(protectedBytes))
                   ?? throw new IOException("分享文件内容无效。");
        }
        catch (Exception error) when (error is CryptographicException or JsonException)
        {
            _logger.LogError(error, "Workspace share cannot be decrypted. Check persistent Data Protection keys.");
            throw new IOException("无法解密分享，请检查持久化 Data Protection 密钥。", error);
        }
    }

    private string GetOwnerDirectory(int ownerId)
    {
        if (ownerId <= 0) throw new ArgumentException("无效的分享所有者。");
        return System.IO.Path.Combine(_root, ownerId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private string GetFilePath(string id, string token)
    {
        if (!ShareId.IsMatch(id) || !int.TryParse(id.Split('-')[0], out var ownerId))
            throw new ArgumentException("无效的分享标识。");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        return System.IO.Path.Combine(GetOwnerDirectory(ownerId), $"{id}-{hash}.protected");
    }

    public void Dispose()
    {
        _publicLimiter.Dispose();
        _gate.Dispose();
    }
}
