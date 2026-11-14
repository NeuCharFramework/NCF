/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceDocumentTools.cs
    文件功能描述：WorkspaceDocumentTools 相关定义与实现


    创建标识：Senparc - 20261026

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Areas.Admin.Domain.Services;

internal sealed class WorkspaceToolBudget
{
    public const int MaxCharacters = 16000;
    private int _used;
    public string Json(object value)
    {
        var text = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        while (true)
        {
            var previous = Volatile.Read(ref _used);
            if (text.Length > MaxCharacters - previous)
                throw new InvalidOperationException("本轮 Workspace 资料输出已达 16000 字符限额。请停止继续读取，明确告知资料尚未读完，或请用户缩小范围／下一轮继续。");
            if (Interlocked.CompareExchange(ref _used, previous + text.Length, previous) == previous) return text;
        }
    }
}

internal sealed class WorkspaceDocumentTools
{
    private readonly IWorkspaceClient _client;
    private readonly WorkspaceDocumentReader _reader;
    private readonly WorkspaceToolBudget _budget;
    public WorkspaceDocumentTools(IWorkspaceClient client, WorkspaceDocumentReader reader, WorkspaceToolBudget budget)
    {
        _client = client;
        _reader = reader;
        _budget = budget;
    }

    public async Task<string> InfoAsync(string relativePath, string expectedETag = "", CancellationToken cancellationToken = default)
    {
        var (document, etag) = await _reader.ReadAsync(_client, relativePath, expectedETag, cancellationToken);
        return _budget.Json(new
        {
            path = relativePath, etag, document.Notice, document.Characters,
            sectionCount = document.Sections.Count,
            sections = document.Sections.Take(20).Select((section, index) => new
            {
                section = index + 1, title = section.Title, characters = section.Text.Length
            }),
            sectionIndexTruncated = document.Sections.Count > 20,
            hasText = document.HasText
        });
    }

    public async Task<string> ReadAsync(string relativePath, int section = 1, int offset = 0,
        int maxCharacters = 4000, string expectedETag = "", CancellationToken cancellationToken = default)
    {
        if (maxCharacters is < 1 or > 4000 || offset < 0 || section < 1)
            throw new ArgumentException("读取参数无效：section 从 1 开始，offset 非负，maxCharacters 为 1–4000。");
        var (document, etag) = await _reader.ReadAsync(_client, relativePath, expectedETag, cancellationToken);
        if (section > document.Sections.Count) throw new ArgumentException("分段超出文档范围，请先调用 workspace_document_info。");
        var part = document.Sections[section - 1];
        if (offset > part.Text.Length) throw new ArgumentException("offset 超过分段长度。");
        var length = Math.Min(maxCharacters, part.Text.Length - offset);
        if (length > 0 && offset + length < part.Text.Length && char.IsHighSurrogate(part.Text[offset + length - 1])) length--;
        return _budget.Json(new
        {
            path = relativePath, etag, section, title = part.Title, offset,
            text = part.Text.Substring(offset, length),
            nextOffset = offset + length, hasMore = offset + length < part.Text.Length,
            sectionCount = document.Sections.Count, document.Notice
        });
    }

    public async Task<string> SearchAsync(string relativePath, string query, string expectedETag = "",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 200)
            throw new ArgumentException("文档搜索关键词必须为 1–200 个字符。");
        var (document, etag) = await _reader.ReadAsync(_client, relativePath, expectedETag, cancellationToken);
        var matches = new List<object>();
        for (var index = 0; index < document.Sections.Count && matches.Count < 10; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var part = document.Sections[index];
            var offset = part.Text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (offset < 0) continue;
            var start = Math.Max(0, offset - 80);
            matches.Add(new
            {
                section = index + 1, title = part.Title, matchOffset = offset, snippetOffset = start,
                snippet = part.Text.Substring(start, Math.Min(240, part.Text.Length - start))
            });
        }
        return _budget.Json(new { path = relativePath, etag, matches, limit = 10, document.Notice });
    }
}
