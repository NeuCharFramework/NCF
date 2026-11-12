/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminChatWorkspaceToolFactory.cs
    文件功能描述：后台对话工作区工具工厂
    
    
    创建标识：Senparc - 20261015
    
    修改标识：Senparc - 20261021
    修改描述：v0.11.1 为后台对话提供工作区工具

    修改标识：Senparc - 20261024
    修改描述：空查询时回退为目录列表，避免 AI 首次浏览 Workspace 失败

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

    修改标识：Senparc - 20261111
    修改描述：补充 Workspace 工具失败诊断、分段写入规则与文件变更摘要

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Senparc.CO2NET.Trace;
using Senparc.Xncf.Workspace.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.IO;

namespace Senparc.Areas.Admin.Domain.Services;

internal static class AdminChatWorkspaceToolFactory
{
    public static IReadOnlyList<AIFunction> Create(
        IWorkspaceClient client,
        bool allowWrite,
        bool allowCli,
        WorkspaceDocumentReader documents = null)
    {
        var budget = new WorkspaceToolBudget();
        var target = new WorkspaceToolTarget(client, allowWrite, allowCli, budget);
        var functions = new List<AIFunction>
        {
            Create(target, nameof(WorkspaceToolTarget.ListAsync), "workspace_list", "列出当前 Workspace 内的文件和目录。"),
            Create(target, nameof(WorkspaceToolTarget.ReadAsync), "workspace_read", "读取当前 Workspace 内的文本文件。"),
            Create(
                target,
                nameof(WorkspaceToolTarget.SearchAsync),
                "workspace_search",
                "在当前 Workspace 中搜索文件名、相对路径 glob 和文件内容。nameQuery 是文件名子串；pathGlob 支持相对路径 glob，例如 tools/NcfSimulatedSite/**/*Workspace*；contentQuery 默认是字面量，设置 contentRegex=true 后支持正则表达式和 | 批量条件，例如 WorkspaceAssets|sandbox|Content-Security-Policy。返回匹配文件、行号和预览；如果未提供查询条件，则返回指定目录的文件和目录列表。")
        };
        if (documents != null)
        {
            var reader = new WorkspaceDocumentTools(client, documents, budget);
            functions.Add(Create(reader, nameof(WorkspaceDocumentTools.InfoAsync), "workspace_document_info", "获取 PDF/Word/Excel/PPT/文本文件的本地文字层分段目录、工作表名称和解析限制；不会调用 OCR 或视觉模型。"));
            functions.Add(Create(reader, nameof(WorkspaceDocumentTools.ReadAsync), "workspace_document_read", "按分段（PDF 页/幻灯片/工作表/正文块）和字符偏移读取资料，每次最多 4000 字符，使用 nextOffset 分页，不能将部分内容声称为全文。"));
            functions.Add(Create(reader, nameof(WorkspaceDocumentTools.SearchAsync), "workspace_document_search", "在本地解析的文档中搜索关键词，最多返回 10 个命中分段及短预览，再按需读取对应分段。"));
        }

        if (allowWrite)
        {
            functions.Add(Create(target, nameof(WorkspaceToolTarget.WriteAsync), "workspace_write", "在当前 Workspace 内写入文本文件。relativePath 必须是非空的文件相对路径；内容较长或传输失败时，应先读取现有文件并使用 workspace_patch 分段追加。"));
            functions.Add(Create(target, nameof(WorkspaceToolTarget.MkdirAsync), "workspace_mkdir", "在当前 Workspace 内创建目录。"));
            functions.Add(Create(target, nameof(WorkspaceToolTarget.PatchAsync), "workspace_patch", "对当前 Workspace 内的文件应用精确文本补丁；匹配数量不符合预期时不会写入。"));
            functions.Add(Create(target, nameof(WorkspaceToolTarget.DeleteAsync), "workspace_delete", "删除当前 Workspace 内的文件或空目录；删除非空目录时必须明确使用 recursive=true。"));
        }

        if (allowCli)
        {
            functions.Add(Create(
                target,
                nameof(WorkspaceToolTarget.ExecutePythonAsync),
                "workspace_execute_python",
                "在当前 Workspace 内执行 Python .py 文件。执行器优先使用 Workspace 内的 .venv，并返回真实退出码、标准输出和错误输出。"));
        }

        return functions
            .Select(function => (AIFunction)new WorkspaceDiagnosticAIFunction(function, client.WorkspaceId))
            .ToList();
    }

    private static AIFunction Create(object target, string methodName, string name, string description)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"Workspace tool method not found: {methodName}");
        return AIFunctionFactory.Create(method, target, name, description);
    }

    private sealed class WorkspaceToolTarget
    {
        private readonly IWorkspaceClient _client;
        private readonly bool _allowWrite;
        private readonly bool _allowCli;
        private readonly WorkspaceToolBudget _budget;

        public WorkspaceToolTarget(IWorkspaceClient client, bool allowWrite, bool allowCli, WorkspaceToolBudget budget)
        {
            _client = client;
            _allowWrite = allowWrite;
            _allowCli = allowCli;
            _budget = budget;
        }

        public async Task<string> ListAsync(string relativePath = "", int offset = 0, int maxEntries = 30,
            CancellationToken cancellationToken = default)
        {
            if (offset < 0 || maxEntries is < 1 or > 100) throw new ArgumentException("目录分页参数无效，最多返回 100 个条目。");
            var entries = await _client.ListAsync(relativePath, cancellationToken).ConfigureAwait(false);
            return _budget.Json(new { entries = entries.Skip(offset).Take(maxEntries), nextOffset = offset + maxEntries,
                hasMore = offset + maxEntries < entries.Count });
        }

        public async Task<string> ReadAsync(string relativePath, int offset = 0, int maxCharacters = 4000,
            CancellationToken cancellationToken = default)
        {
            EnsureFilePath(relativePath);
            if (offset < 0 || maxCharacters is < 1 or > 4000) throw new ArgumentException("文本读取参数无效，每次最多 4000 字符。");
            var extension = Path.GetExtension(relativePath).ToLowerInvariant();
            if (extension is ".pdf" or ".docx" or ".xlsx" or ".pptx" or ".doc" or ".xls" or ".ppt"
                or ".jpg" or ".jpeg" or ".png" or ".webp" or ".zip")
                throw new ArgumentException("不能将二进制文件作为 UTF-8 文本读取。文档请使用 workspace_document_info/read；图片必须由用户明确启用视觉处理。");
            var file = await _client.ReadFileAsync(relativePath, cancellationToken: cancellationToken).ConfigureAwait(false);
            var text = new System.Text.UTF8Encoding(false, true).GetString(file.Content);
            if (text.Contains('\0')) throw new ArgumentException("文件包含二进制内容，不能作为文本读取。");
            if (offset > text.Length) throw new ArgumentException("offset 超过文件长度。");
            var count = Math.Min(maxCharacters, text.Length - offset);
            if (count > 0 && offset + count < text.Length && char.IsHighSurrogate(text[offset + count - 1])) count--;
            return _budget.Json(new { content = text.Substring(offset, count), etag = file.Metadata.ETag,
                nextOffset = offset + count, hasMore = offset + count < text.Length });
        }

        public async Task<string> SearchAsync(
            string nameQuery = "",
            string contentQuery = "",
            string relativePath = "",
            bool caseSensitive = false,
            int maxResults = 100,
            string pathGlob = "",
            bool contentRegex = false)
        {
            if (maxResults is < 1 or > 100) throw new ArgumentException("文件搜索最多返回 100 个结果。");
            if (string.IsNullOrWhiteSpace(nameQuery)
                && string.IsNullOrWhiteSpace(contentQuery)
                && string.IsNullOrWhiteSpace(pathGlob))
            {
                return await ListAsync(relativePath).ConfigureAwait(false);
            }

            var results = await _client.SearchAsync(
                new WorkspaceSearchRequest(
                    nameQuery,
                    contentQuery,
                    relativePath,
                    caseSensitive,
                    maxResults,
                    PathGlob: pathGlob,
                    ContentRegex: contentRegex)).ConfigureAwait(false);
            return _budget.Json(results);
        }

        public async Task<string> WriteAsync(string relativePath, string content)
        {
            EnsureWrite();
            EnsureFilePath(relativePath);
            ArgumentNullException.ThrowIfNull(content);
            var before = await SnapshotAsync(relativePath).ConfigureAwait(false);
            var result = await _client.WriteFileAsync(
                new WorkspaceWriteFileRequest(
                    relativePath,
                    System.Text.Encoding.UTF8.GetBytes(content))).ConfigureAwait(false);
            var after = await SnapshotAsync(relativePath).ConfigureAwait(false);
            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "写入成功。",
                change = CreateChange(
                    relativePath,
                    result.Created ? "added" : "modified",
                    before,
                    after)
            });
        }

        public async Task<string> MkdirAsync(string relativePath)
        {
            EnsureWrite();
            EnsureFilePath(relativePath);
            await _client.CreateDirectoryAsync(relativePath).ConfigureAwait(false);
            return "目录创建成功。";
        }

        public async Task<string> PatchAsync(
            string relativePath,
            string oldText,
            string newText,
            int expectedReplacements = 1,
            string expectedSha256 = "")
        {
            EnsureWrite();
            EnsureFilePath(relativePath);
            var before = await SnapshotAsync(relativePath).ConfigureAwait(false);
            var result = await _client.ApplyPatchAsync(
                new WorkspacePatchRequest(
                    relativePath,
                    oldText,
                    newText,
                    expectedReplacements,
                    expectedSha256)).ConfigureAwait(false);
            if (!result.Applied)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    result
                });
            }

            var after = await SnapshotAsync(relativePath).ConfigureAwait(false);
            return JsonSerializer.Serialize(new
            {
                success = true,
                result,
                change = CreateChange(relativePath, "modified", before, after)
            });
        }

        public async Task<string> DeleteAsync(
            string relativePath,
            bool recursive = false,
            string expectedETag = "")
        {
            EnsureWrite();
            EnsureFilePath(relativePath);
            var before = await SnapshotAsync(relativePath).ConfigureAwait(false);
            await _client.DeleteEntryAsync(
                new WorkspaceDeleteEntryRequest(relativePath, recursive, expectedETag)).ConfigureAwait(false);
            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "删除成功。",
                change = CreateChange(relativePath, "deleted", before, WorkspaceFileSnapshot.Missing)
            });
        }

        public async Task<string> ExecutePythonAsync(
            string scriptPath,
            string[] arguments = null,
            string workingDirectory = "")
        {
            if (!_allowCli)
            {
                throw new InvalidOperationException("当前会话未启用 Workspace Python 执行权限。");
            }

            var result = await _client.ExecutePythonAsync(
                new WorkspacePythonRequest(scriptPath, arguments ?? Array.Empty<string>(), workingDirectory)).ConfigureAwait(false);
            return JsonSerializer.Serialize(result);
        }

        private void EnsureWrite()
        {
            if (!_allowWrite)
            {
                throw new InvalidOperationException("当前会话未启用 Workspace 写入权限。");
            }
        }

        private static void EnsureFilePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException(
                    "relativePath 不能为空；即使在 Workspace 根目录写文件，也必须提供文件名，例如 index.html。",
                    nameof(relativePath));
            }
        }

        private async Task<WorkspaceFileSnapshot> SnapshotAsync(string relativePath)
        {
            try
            {
                var metadata = await _client.GetFileInfoAsync(relativePath).ConfigureAwait(false);
                if (metadata.IsDirectory)
                {
                    throw new IOException("Workspace file target is a directory.");
                }

                try
                {
                    var file = await _client.ReadFileAsync(relativePath).ConfigureAwait(false);
                    return new WorkspaceFileSnapshot(
                        true,
                        new UTF8Encoding(false, true).GetString(file.Content),
                        file.Metadata.Length);
                }
                catch (WorkspaceLimitException)
                {
                    return new WorkspaceFileSnapshot(true, null, metadata.Length);
                }
                catch (DecoderFallbackException)
                {
                    return new WorkspaceFileSnapshot(true, null, metadata.Length);
                }
            }
            catch (Exception exception) when (
                exception is FileNotFoundException or DirectoryNotFoundException)
            {
                return WorkspaceFileSnapshot.Missing;
            }
        }

        private static object CreateChange(
            string relativePath,
            string changeType,
            WorkspaceFileSnapshot before,
            WorkspaceFileSnapshot after)
        {
            var diff = (!before.Exists || before.Text != null)
                && (!after.Exists || after.Text != null)
                ? CountLineChanges(before.Exists ? before.Text : string.Empty, after.Exists ? after.Text : string.Empty)
                : ((int Added, int Removed, bool Exact)?)null;
            return new
            {
                path = relativePath,
                changeType,
                beforeBytes = before.Exists ? before.Length : 0,
                afterBytes = after.Exists ? after.Length : 0,
                addedLines = diff?.Added,
                removedLines = diff?.Removed,
                lineCountsExact = diff?.Exact
            };
        }

        private static (int Added, int Removed, bool Exact) CountLineChanges(
            string before,
            string after)
        {
            var beforeLines = SplitLines(before);
            var afterLines = SplitLines(after);
            if (beforeLines.Length > 5000 || afterLines.Length > 5000)
            {
                return (afterLines.Length, beforeLines.Length, false);
            }

            var next = new int[afterLines.Length + 1];
            var current = new int[afterLines.Length + 1];
            for (var beforeIndex = beforeLines.Length - 1; beforeIndex >= 0; beforeIndex--)
            {
                current[afterLines.Length] = 0;
                for (var afterIndex = afterLines.Length - 1; afterIndex >= 0; afterIndex--)
                {
                    current[afterIndex] = string.Equals(
                        beforeLines[beforeIndex],
                        afterLines[afterIndex],
                        StringComparison.Ordinal)
                        ? next[afterIndex + 1] + 1
                        : Math.Max(next[afterIndex], current[afterIndex + 1]);
                }

                (next, current) = (current, next);
            }

            var commonLines = next[0];
            return (afterLines.Length - commonLines, beforeLines.Length - commonLines, true);
        }

        private static string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Array.Empty<string>();
            }

            var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n');
            return lines.Length > 0 && lines[^1].Length == 0
                ? lines[..^1]
                : lines;
        }

        private sealed record WorkspaceFileSnapshot(bool Exists, string Text, long Length)
        {
            public static WorkspaceFileSnapshot Missing { get; } = new(false, null, 0);
        }
    }

    private sealed class WorkspaceDiagnosticAIFunction : DelegatingAIFunction
    {
        private readonly string _workspaceId;

        public WorkspaceDiagnosticAIFunction(AIFunction innerFunction, string workspaceId)
            : base(innerFunction)
        {
            _workspaceId = workspaceId ?? string.Empty;
        }

        protected override async ValueTask<object> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var argumentSummary = FormatArguments(arguments);
            try
            {
                return await base.InvokeCoreAsync(arguments, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var root = exception.GetBaseException();
                var detail =
                    $"WorkspaceId={_workspaceId}; Tool={Name}; Arguments={argumentSummary}; " +
                    $"Error={root.GetType().FullName}: {root.Message}";
                SenparcTrace.SendCustomLog("AdminChat.WorkspaceTool.Failed", detail);
                SenparcTrace.BaseExceptionLog(root);
                Console.Error.WriteLine($"[AdminChat.WorkspaceTool.Failed] {detail}");
                throw;
            }
        }

        private static string FormatArguments(IEnumerable<KeyValuePair<string, object>> arguments)
        {
            if (arguments == null)
            {
                return "(none)";
            }

            var values = arguments
                .Select(pair => $"{pair.Key}={FormatArgumentValue(pair.Key, pair.Value)}")
                .ToList();
            return values.Count == 0 ? "(none)" : string.Join(", ", values);
        }

        private static string FormatArgumentValue(string key, object value)
        {
            if (key.Contains("content", StringComparison.OrdinalIgnoreCase))
            {
                return value is string text
                    ? $"[text length={text.Length}]"
                    : $"[{value?.GetType().Name ?? "null"}]";
            }

            var textValue = value?.ToString() ?? "null";
            textValue = textValue.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return textValue.Length <= 500 ? textValue : textValue[..500] + "...";
        }
    }
}
