/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatWorkspaceAssetService.cs
    文件功能描述：AdminChatWorkspaceAssetService 相关定义与实现


    创建标识：Senparc - 20261024

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Xncf.Workspace;
using Senparc.Xncf.Workspace.Abstractions;
using Microsoft.Extensions.Options;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record WorkspaceAssetDownload(byte[] Bytes, string FileName);

public sealed class AdminChatWorkspaceAssetService
{
    public const int MaxArchiveEntries = 1000;
    public const long MaxArchiveBytes = 64 * 1024 * 1024;
    private readonly AdminChatSessionService _sessions;
    private readonly IServiceProvider _services;
    private readonly WorkspaceAssetPreviewBuilder _previewBuilder;

    public AdminChatWorkspaceAssetService(
        AdminChatSessionService sessions, IServiceProvider services, WorkspaceAssetPreviewBuilder previewBuilder)
    {
        _sessions = sessions;
        _services = services;
        _previewBuilder = previewBuilder;
    }

    public IWorkspaceStorage Storage => _services.GetService<IWorkspaceStorage>()
        ?? throw new InvalidOperationException("Workspace 模块尚不可用。");

    public async Task<AdminChatSession> GetSessionAsync(
        int sessionId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var isSuperAdmin = user.Claims.Where(claim => claim.Type == ClaimTypes.Role)
            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Any(role => string.Equals(role.Trim(), Ncf.Service.Config.SYSROLE_ADMINISTRATOR_ROLE_CODE,
                StringComparison.OrdinalIgnoreCase));
        if (user.Identity?.IsAuthenticated != true || !isSuperAdmin
            || !int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var ownerId) || ownerId <= 0)
            throw new UnauthorizedAccessException("只有有权限的超级管理员可以访问 Workspace 资产。");
        var session = await _sessions.GetSessionByIdAsync(sessionId, ownerId);
        if (session == null || string.IsNullOrEmpty(session.WorkspaceId))
            throw new FileNotFoundException("会话或 Workspace 不存在。");
        var workspace = await Storage.GetAsync(session.WorkspaceId, cancellationToken);
        if (workspace == null || workspace.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Workspace 不存在或不属于当前管理员。");
        return session;
    }

    public Task<WorkspaceHtmlPreview> BuildPreviewAsync(
        string workspaceId, string path, CancellationToken cancellationToken) =>
        _previewBuilder.BuildAsync(Storage, workspaceId, path, cancellationToken);

    public async Task<bool> IsShareWorkspaceAvailableAsync(WorkspaceAssetShare share, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetSessionByIdAsync(share.SessionId, share.OwnerId);
        if (session?.WorkspaceId != share.WorkspaceId) return false;
        var workspace = await Storage.GetAsync(share.WorkspaceId, cancellationToken);
        return workspace?.OwnerId == share.OwnerId;
    }

    public async Task<WorkspaceAssetDownload> DownloadAsync(
        string workspaceId, string path, CancellationToken cancellationToken)
    {
        path = WorkspaceAssetPreviewBuilder.NormalizePath(path, allowRoot: true);
        var metadata = await Storage.GetFileInfoAsync(workspaceId, path, cancellationToken);
        var fileLimit = Math.Min((int)MaxArchiveBytes,
            _services.GetRequiredService<IOptions<WorkspaceOptions>>().Value.MaxFileReadBytes);
        if (!metadata.IsDirectory)
        {
            var file = await Storage.ReadFileBytesAsync(workspaceId, path, fileLimit, cancellationToken);
            return new WorkspaceAssetDownload(file.Content, path.Split('/').Last());
        }

        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var directories = new Queue<(string Path, int Depth)>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            directories.Enqueue((path, 0));
            long totalBytes = 0;
            var count = 0;
            var listLimit = _services.GetRequiredService<IOptions<WorkspaceOptions>>().Value.MaxEntriesPerList;
            while (directories.TryDequeue(out var directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (directory.Depth > 32) throw new InvalidOperationException("目录嵌套超过 32 层，无法打包。");
                var entries = await Storage.ListAsync(workspaceId, directory.Path, cancellationToken);
                if (entries.Count >= listLimit)
                    throw new InvalidOperationException("目录列表可能被截断，请选择较小的子目录下载。");
                foreach (var entry in entries)
                {
                    var entryPath = WorkspaceAssetPreviewBuilder.NormalizePath(entry.RelativePath);
                    var prefix = directory.Path.Length == 0 ? "" : directory.Path + "/";
                    if (!entryPath.StartsWith(prefix, StringComparison.Ordinal)
                        || entryPath[prefix.Length..].Contains('/') || !seen.Add(entryPath))
                        throw new IOException("Workspace 返回了无效的目录条目。");
                    if (++count > MaxArchiveEntries)
                        throw new InvalidOperationException("ZIP 最多包含 1000 个文件和目录，请选择较小的子目录。");
                    var zipPath = path.Length == 0 ? entryPath : entryPath[(path.Length + 1)..];
                    if (entry.IsDirectory)
                    {
                        archive.CreateEntry(zipPath + "/");
                        directories.Enqueue((entryPath, directory.Depth + 1));
                    }
                    else
                    {
                        if (entry.Length > fileLimit || entry.Length > MaxArchiveBytes - totalBytes)
                            throw new InvalidOperationException("文件超过 Workspace 读取限制或 ZIP 原始内容超过 64 MB。");
                        var file = await Storage.ReadFileBytesAsync(workspaceId, entryPath, fileLimit, cancellationToken);
                        totalBytes += file.Content.Length;
                        if (totalBytes > MaxArchiveBytes) throw new InvalidOperationException("ZIP 原始内容超过 64 MB。");
                        await using var target = archive.CreateEntry(zipPath, CompressionLevel.Fastest).Open();
                        await target.WriteAsync(file.Content, cancellationToken);
                    }
                }
            }
        }
        var name = path.Length == 0 ? "workspace" : path.Split('/').Last();
        return new WorkspaceAssetDownload(output.ToArray(), name + ".zip");
    }
}
