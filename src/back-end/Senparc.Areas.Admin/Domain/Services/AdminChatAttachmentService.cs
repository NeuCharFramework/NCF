/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatAttachmentService.cs
    文件功能描述：AdminChatAttachmentService 相关定义与实现


    创建标识：Senparc - 20261026

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add bilingual contextual documentation help with iframe dialog and new-window access

----------------------------------------------------------------*/

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.Workspace;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class AdminChatAttachmentService
{
    public const string AttachmentOnlyMessage = "请先查看本轮附件的目录和解析限制，并询问我希望如何处理。";
    public const int MaxAttachments = 4;
    public const int MaxVisionBytes = 512 * 1024;
    public static readonly string[] AllowedExtensions =
        [".txt", ".md", ".csv", ".tsv", ".json", ".docx", ".xlsx", ".pptx", ".pdf", ".doc", ".xls", ".ppt", ".png", ".jpg", ".jpeg", ".webp"];
    private readonly IServiceProvider _services;
    private IWorkspaceStorage Storage => _services.GetService<IWorkspaceStorage>()
        ?? throw new InvalidOperationException("Workspace 模块不可用。");
    private readonly WorkspaceDocumentReader _documents;
    private readonly WorkspaceOptions _options;
    private readonly ILogger<AdminChatAttachmentService> _logger;

    public AdminChatAttachmentService(IServiceProvider services, WorkspaceDocumentReader documents, IOptions<WorkspaceOptions> options,
        ILogger<AdminChatAttachmentService> logger)
    {
        _services = services;
        _documents = documents;
        _options = options.Value;
        _logger = logger;
    }

    public int UploadLimit => Math.Min(_documents.ReadLimit, _options.MaxFileWriteBytes);
    public long LargeUploadLimit => _options.MaxUploadFileBytes;

    public async Task<AdminChatAttachment> UploadAsync(
        AdminChatSession session, IFormFile file, IFormFile visionPreview, CancellationToken cancellationToken)
    {
        if (file == null || file.Length <= 0 || file.Length > UploadLimit)
            throw new ArgumentException($"附件不能为空，单文件最大 {UploadLimit / 1024} KB（遵循 Workspace 配置）。");
        var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (name.Length == 0 || name.Length > 120 || name.Any(char.IsControl) || name.Contains(':'))
            throw new ArgumentException("附件文件名无效或超过 120 个字符。");
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension)) throw new ArgumentException("不支持此附件类型，不允许上传可执行脚本或宏文件。");
        var bytes = await ReadUploadAsync(file, UploadLimit, cancellationToken);
        ValidateSignature(bytes, extension);
        WorkspaceDocument document = null;
        if (NcfFileTextExtractor.CanExtractText(extension) || extension == ".pdf")
            document = await _documents.ParseAsync(bytes, extension, cancellationToken);
        byte[] preview = null;
        var image = IsImage(extension);
        if (visionPreview != null)
        {
            if (!image) throw new ArgumentException("只有图片可以提供视觉预览。");
            preview = await ReadUploadAsync(visionPreview, MaxVisionBytes, cancellationToken);
            ValidateVisionJpeg(preview);
        }
        var directory = "uploads/" + Guid.NewGuid().ToString("N");
        var path = directory + "/" + name;
        await Storage.CreateDirectoryAsync(session.WorkspaceId, directory, cancellationToken);
        try
        {
            var result = await Storage.WriteFileBytesAsync(session.WorkspaceId,
                new WorkspaceWriteFileRequest(path, bytes, Overwrite: false), cancellationToken);
            var attachment = Describe(path, result.Metadata, document);
            if (preview != null)
            {
                var vision = await Storage.WriteFileBytesAsync(session.WorkspaceId,
                    new WorkspaceWriteFileRequest(directory + "/.vision.jpg", preview, Overwrite: false), cancellationToken);
                attachment.VisionETag = vision.Metadata.ETag;
            }
            if (document != null) _documents.Remember(session.WorkspaceId, path, result.Metadata.ETag, document);
            return attachment;
        }
        catch (Exception uploadError)
        {
            _logger.LogError(uploadError, "Workspace attachment upload failed. Session={SessionId}", session.Id);
            try
            {
                await Storage.DeleteEntryAsync(session.WorkspaceId,
                    new WorkspaceDeleteEntryRequest(directory, Recursive: true), CancellationToken.None);
            }
            catch (Exception cleanupError)
            {
                _logger.LogError(cleanupError, "Workspace attachment rollback failed. Workspace={WorkspaceId}, Directory={Directory}",
                    session.WorkspaceId, directory);
            }
            throw;
        }
    }

    public async Task<AdminChatAttachment> UploadLargeAsync(
        AdminChatSession session, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length <= 0 || file.Length > LargeUploadLimit)
            throw new ArgumentException($"附件不能为空，单文件最大 {LargeUploadLimit / (1024 * 1024)} MiB。");

        var (name, extension) = ValidateFileName(file);
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("不支持此附件类型，不允许上传可执行脚本或宏文件。");

        var directory = "uploads/" + Guid.NewGuid().ToString("N");
        var path = directory + "/" + name;
        await Storage.CreateDirectoryAsync(session.WorkspaceId, directory, cancellationToken);
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await Storage.WriteFileStreamAsync(
                session.WorkspaceId,
                new WorkspaceStreamWriteRequest(path, Overwrite: false, ContentLength: file.Length),
                stream,
                cancellationToken);
            return Describe(path, result.Metadata, document: null, deferredProcessing: true);
        }
        catch (Exception uploadError)
        {
            _logger.LogError(uploadError, "Large Workspace attachment upload failed. Session={SessionId}", session.Id);
            try
            {
                await Storage.DeleteEntryAsync(session.WorkspaceId,
                    new WorkspaceDeleteEntryRequest(directory, Recursive: true), CancellationToken.None);
            }
            catch (Exception cleanupError)
            {
                _logger.LogError(cleanupError, "Large Workspace attachment rollback failed. Workspace={WorkspaceId}, Directory={Directory}",
                    session.WorkspaceId, directory);
            }
            throw;
        }
    }

    public async Task DeletePendingAsync(
        AdminChatSession session,
        AdminChatAttachmentInput input,
        IReadOnlyList<AdminChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(session.WorkspaceId))
        {
            throw new ArgumentException("附件 Workspace 不存在。");
        }

        var workspace = await Storage.GetAsync(session.WorkspaceId, cancellationToken).ConfigureAwait(false);
        if (workspace?.OwnerId != session.UserId)
        {
            throw new UnauthorizedAccessException("无权删除此 Workspace 的附件。");
        }

        var (path, directory) = ValidateAttachmentPath(input.RelativePath, input.ETag);
        if (messages?.SelectMany(message => message.GetAttachments())
            .Any(attachment => string.Equals(attachment.RelativePath, path, StringComparison.Ordinal)) == true)
        {
            throw new InvalidOperationException("已经提交到聊天消息的附件不能从 Workspace 中删除。");
        }

        var metadata = await Storage.GetFileInfoAsync(
            session.WorkspaceId,
            path,
            cancellationToken).ConfigureAwait(false);
        if (metadata.IsDirectory || !string.Equals(metadata.ETag, input.ETag, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("附件已改变或不存在，请刷新后重试。");
        }

        await Storage.DeleteEntryAsync(
            session.WorkspaceId,
            new WorkspaceDeleteEntryRequest(directory, Recursive: true),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AdminChatAttachment>> ValidateAsync(
        AdminChatSession session, IReadOnlyList<AdminChatAttachmentInput> inputs, CancellationToken cancellationToken = default)
    {
        if (inputs == null || inputs.Count == 0) return Array.Empty<AdminChatAttachment>();
        if (inputs.Count > MaxAttachments) throw new ArgumentException("每条消息最多附加 4 个文件。");
        if (string.IsNullOrEmpty(session.WorkspaceId)) throw new ArgumentException("上传资料需要先选择 Workspace。");
        var workspace = await Storage.GetAsync(session.WorkspaceId, cancellationToken);
        if (workspace?.OwnerId != session.UserId) throw new UnauthorizedAccessException("无权使用此 Workspace 的附件。");
        await using var client = CreateClient(session);
        var result = new List<AdminChatAttachment>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var input in inputs)
        {
            var path = WorkspaceAssetPreviewBuilder.NormalizePath(input.RelativePath);
            var segments = path.Split('/');
            if (segments.Length != 3 || segments[0] != "uploads" || !Guid.TryParseExact(segments[1], "N", out _)
                || segments[2] == ".vision.jpg" || !seen.Add(path) || string.IsNullOrEmpty(input.ETag))
                throw new ArgumentException("附件引用无效或重复，请重新上传。");
            var metadata = await client.GetFileInfoAsync(path, cancellationToken);
            if (metadata.IsDirectory || metadata.ETag != input.ETag)
                throw new InvalidOperationException("附件已改变或不存在，请重新选择。");
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension)) throw new ArgumentException("不支持此附件类型。");
            var deferredProcessing = metadata.Length > _documents.ReadLimit;
            WorkspaceDocument document = null;
            if (!deferredProcessing && (NcfFileTextExtractor.CanExtractText(extension) || extension == ".pdf"))
                document = (await _documents.ReadAsync(client, path, input.ETag, cancellationToken)).Document;
            var item = Describe(path, metadata, document, deferredProcessing);
            if (!string.IsNullOrEmpty(input.VisionETag))
            {
                if (!IsImage(extension)) throw new ArgumentException("文档附件不能作为图片发送。");
                var visionMetadata = await client.GetFileInfoAsync(VisionPath(path), cancellationToken);
                if (visionMetadata.ETag != input.VisionETag) throw new InvalidOperationException("视觉预览已改变，请重新上传。");
                item.VisionETag = visionMetadata.ETag;
            }
            if (input.UseVision && !item.CanUseVision) throw new ArgumentException("图片没有可用的视觉预览，请重新上传。");
            item.UseVision = input.UseVision;
            result.Add(item);
        }
        if (result.Count(item => item.UseVision) > 2) throw new ArgumentException("每条消息最多启用 2 张图片的视觉处理。");
        return result;
    }

    private static (string Path, string Directory) ValidateAttachmentPath(string relativePath, string etag)
    {
        var path = WorkspaceAssetPreviewBuilder.NormalizePath(relativePath);
        var segments = path.Split('/');
        if (segments.Length != 3
            || segments[0] != "uploads"
            || !Guid.TryParseExact(segments[1], "N", out _)
            || segments[2] == ".vision.jpg"
            || string.IsNullOrWhiteSpace(etag))
        {
            throw new ArgumentException("附件引用无效，请重新上传。");
        }

        return (path, path[..path.LastIndexOf('/')]);
    }

    public async Task<IList<ChatMessage>> BuildVisionMessagesAsync(
        int sessionId, int ownerId, string prompt, IReadOnlyList<AdminChatAttachment> attachments,
        AdminChatSessionService sessions, CancellationToken cancellationToken)
    {
        var content = new List<AIContent> { new TextContent(prompt) };
        if (attachments?.Any(attachment => attachment.UseVision) == true)
        {
            var session = await sessions.GetSessionByIdAsync(sessionId, ownerId)
                ?? throw new UnauthorizedAccessException("附件会话不可访问。");
            var workspace = await Storage.GetAsync(session.WorkspaceId, cancellationToken);
            if (workspace?.OwnerId != ownerId) throw new UnauthorizedAccessException("附件 Workspace 不可访问。");
            foreach (var attachment in attachments.Where(attachment => attachment.UseVision))
            {
                var file = await Storage.ReadFileBytesAsync(session.WorkspaceId, VisionPath(attachment.RelativePath),
                    MaxVisionBytes, cancellationToken);
                if (file.Metadata.ETag != attachment.VisionETag)
                    throw new InvalidOperationException("视觉预览已变化，请重新上传，未向模型发送图片。");
                ValidateVisionJpeg(file.Content);
                content.Add(new TextContent($"用户已明确授权视觉处理的图片：{attachment.Name}。以下图片为缩小的无 EXIF 视觉预览，原文件仍在 Workspace。"));
                content.Add(new DataContent(file.Content, "image/jpeg") { Name = attachment.Name });
            }
        }
        return new List<ChatMessage> { new(ChatRole.User, content) };
    }

    private IWorkspaceClient CreateClient(AdminChatSession session) => _services.GetRequiredService<IWorkspaceClientFactory>().Create(new WorkspaceAccessContext(
        session.WorkspaceId, new WorkspacePolicy(AllowRead: true, MaxBytes: UploadLimit),
        "AdminChatAttachments", session.UserId.ToString(), session.Id.ToString()));

    private static (string Name, string Extension) ValidateFileName(IFormFile file)
    {
        var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (name.Length == 0 || name.Length > 120 || name.Any(char.IsControl) || name.Contains(':'))
            throw new ArgumentException("附件文件名无效或超过 120 个字符。");
        return (name, Path.GetExtension(name).ToLowerInvariant());
    }

    private static string VisionPath(string path) => path[..path.LastIndexOf('/')] + "/.vision.jpg";
    private static bool IsImage(string extension) => extension is ".png" or ".jpg" or ".jpeg" or ".webp";

    private static AdminChatAttachment Describe(
        string path, WorkspaceFileMetadata metadata, WorkspaceDocument document, bool deferredProcessing = false)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return new AdminChatAttachment
        {
            RelativePath = path, Name = path.Split('/').Last(), Size = metadata.Length, ETag = metadata.ETag,
            ContentType = IsImage(extension) ? "image/" + (extension is ".jpg" or ".jpeg" ? "jpeg" : extension[1..])
                : extension == ".pdf" ? "application/pdf" : "application/octet-stream",
            LocalStatus = deferredProcessing ? "已保存，需使用 Workspace/Python 处理"
                : document != null ? document.HasText ? "已本地解析"
                    : extension == ".pdf" ? "未提取到文字，可能需要 OCR" : "未提取到可读文字"
                : IsImage(extension) ? "图片已保存，默认不发给模型" : "仅保存，需转换为新版 Office 格式",
            SectionCount = document?.Sections.Count ?? 0, ExtractedCharacters = document?.Characters ?? 0,
            Notice = deferredProcessing
                ? "文件较大，服务端未读取全文。请让 AI 使用 Workspace 工具编写 Python，采用流式或分批方式处理；文件全文不会自动发送给模型。"
                : document?.Notice ?? (IsImage(extension)
                ? "勾选视觉后仅发送压缩预览。需要选择支持图片的模型；可能产生额外视觉 Token。"
                : "未解析旧版二进制 Office，未执行其中的宏；请另存为 DOCX/XLSX/PPTX。")
        };
    }

    private static async Task<byte[]> ReadUploadAsync(IFormFile file, int limit, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > limit) throw new ArgumentException("上传文件超过允许大小。");
        using var memory = new MemoryStream();
        await using var stream = file.OpenReadStream();
        var buffer = new byte[65536];
        int count;
        while ((count = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (memory.Length + count > limit) throw new ArgumentException("上传文件超过允许大小。");
            await memory.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
        }
        return memory.ToArray();
    }

    private static void ValidateSignature(byte[] bytes, string extension)
    {
        bool match = extension switch
        {
            ".pdf" => bytes.AsSpan().StartsWith("%PDF-"u8),
            ".docx" or ".xlsx" or ".pptx" => bytes.AsSpan().StartsWith(new byte[] { 0x50, 0x4b, 3, 4 }),
            ".doc" or ".xls" or ".ppt" => bytes.AsSpan().StartsWith(new byte[] { 0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1 }),
            ".png" => bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".jpg" or ".jpeg" => bytes.AsSpan().StartsWith(new byte[] { 0xff, 0xd8, 0xff }),
            ".webp" => bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => true
        };
        if (!match) throw new InvalidDataException("文件内容与扩展名不符，已拒绝上传。");
    }

    public static void ValidateVisionJpeg(byte[] bytes)
    {
        if (bytes.Length > MaxVisionBytes) throw new ArgumentException("视觉预览不能超过 512 KB。");
        ValidateSignature(bytes, ".jpg");
        for (var offset = 2; offset + 8 < bytes.Length;)
        {
            if (bytes[offset++] != 0xff) throw new InvalidDataException("视觉 JPEG 结构无效。");
            while (offset < bytes.Length && bytes[offset] == 0xff) offset++;
            if (offset >= bytes.Length) break;
            var marker = bytes[offset++];
            if (marker is 0xd8 or 0x01 || marker is >= 0xd0 and <= 0xd7) continue;
            if (offset + 2 > bytes.Length) break;
            var length = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
            if (length < 2 || offset + length > bytes.Length) break;
            if (marker is 0xc0 or 0xc1 or 0xc2)
            {
                if (length < 8) break;
                var height = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 3, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 5, 2));
                if (width is < 1 or > 1536 || height is < 1 or > 1536)
                    throw new ArgumentException("视觉预览的长边不能超过 1536 像素。");
                return;
            }
            if (marker is 0xda or 0xd9) break;
            offset += length;
        }
        throw new InvalidDataException("无法验证视觉 JPEG 尺寸。");
    }
}
