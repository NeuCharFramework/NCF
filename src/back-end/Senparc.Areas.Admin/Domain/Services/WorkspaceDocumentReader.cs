/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceDocumentReader.cs
    文件功能描述：WorkspaceDocumentReader 相关定义与实现


    创建标识：Senparc - 20261026

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.Workspace;
using Senparc.Xncf.Workspace.Abstractions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record WorkspaceDocument(
    IReadOnlyList<NcfFileTextSection> Sections, string Notice)
{
    public int Characters => Sections.Sum(section => section.Text.Length);
    public bool HasText => Sections.Any(section => !string.IsNullOrWhiteSpace(section.Text));
}

public sealed class WorkspaceDocumentReader : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 4_000_000 });
    private readonly SemaphoreSlim _parsing = new(2, 2);
    private readonly WorkspaceOptions _options;

    public WorkspaceDocumentReader(IOptions<WorkspaceOptions> options) => _options = options.Value;

    public int ReadLimit => Math.Min(16 * 1024 * 1024, _options.MaxFileReadBytes);

    public async Task<(WorkspaceDocument Document, string ETag)> ReadAsync(
        IWorkspaceClient client, string path, string expectedETag = "", CancellationToken cancellationToken = default)
    {
        var metadata = await client.GetFileInfoAsync(path, cancellationToken);
        if (metadata.IsDirectory) throw new ArgumentException("请选择文件，而不是目录。");
        if (!string.IsNullOrEmpty(expectedETag) && metadata.ETag != expectedETag)
            throw new InvalidOperationException("文件已发生变化，请重新选择附件，避免使用过期资料。");
        var key = (client.WorkspaceId, path, metadata.ETag);
        if (_cache.TryGetValue(key, out WorkspaceDocument cached)) return (cached, metadata.ETag);
        var file = await client.ReadFileAsync(path, ReadLimit, cancellationToken);
        if (file.Metadata.ETag != metadata.ETag)
            throw new InvalidOperationException("读取期间文件已改变，请重试。");
        var document = await ParseAsync(file.Content, Path.GetExtension(path), cancellationToken);
        Remember(client.WorkspaceId, path, file.Metadata.ETag, document);
        return (document, file.Metadata.ETag);
    }

    public void Remember(string workspaceId, string path, string etag, WorkspaceDocument document)
    {
        _cache.Set((workspaceId, path, etag), document, new MemoryCacheEntryOptions
        {
            Size = Math.Max(1, document.Characters), SlidingExpiration = TimeSpan.FromMinutes(20)
        });
    }

    public async Task<WorkspaceDocument> ParseAsync(byte[] bytes, string extension, CancellationToken cancellationToken)
    {
        if (bytes.Length > ReadLimit) throw new InvalidDataException("文件超过 Workspace 文档读取限制。");
        extension = extension.ToLowerInvariant();
        await _parsing.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (extension == ".pdf")
            {
                using var pdf = PdfDocument.Open(bytes, new ParsingOptions { MaxStackDepth = 64 });
                if (pdf.NumberOfPages > 200) throw new InvalidDataException("PDF 超过 200 页，请拆分后上传。");
                var sections = new List<NcfFileTextSection>();
                var length = 0;
                foreach (var page in pdf.GetPages())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var text = ContentOrderTextExtractor.GetText(page);
                    length += text.Length;
                    if (length > NcfFileTextExtractor.MaxExtractedCharacters)
                        throw new InvalidDataException("PDF 文字超过本地提取限制。");
                    sections.Add(new NcfFileTextSection($"PDF 第 {page.Number} 页", text));
                }
                return new WorkspaceDocument(sections,
                    sections.Any(section => string.IsNullOrWhiteSpace(section.Text))
                        ? "部分或全部页面没有文字层，可能是扫描件。未执行 OCR；需要时请上传页面截图并明确启用视觉模型。"
                        : "仅提取 PDF 文字层，不识别图片、图表或复杂版式；未执行 OCR。");
            }
            if (!NcfFileTextExtractor.CanExtractText(extension))
                throw new NotSupportedException("此格式仅保存原文件，不支持本地提取；旧版 Office 请另存为 DOCX/XLSX/PPTX。");
            return new WorkspaceDocument(NcfFileTextExtractor.ExtractSections(bytes, extension),
                extension == ".xlsx"
                    ? "保留工作表、单元格地址和原始值；公式只读缓存值，不重新计算。日期/货币格式、图片和图表未渲染，不能将缺失或缓存数据当作实时计算结果。"
                    : "仅提取文本；不识别嵌入图片、图表或版式。DOCX 使用正文分段，不声称还原物理页码。");
        }
        catch (Exception error) when (error is UglyToad.PdfPig.Core.PdfDocumentFormatException
            or UglyToad.PdfPig.Exceptions.PdfDocumentEncryptedException)
        {
            throw new InvalidDataException("PDF 损坏、加密或结构不受支持，无法可靠提取文字；请移除密码或重新导出 PDF。", error);
        }
        finally { _parsing.Release(); }
    }

    public void Dispose()
    {
        _cache.Dispose();
        _parsing.Dispose();
    }
}
