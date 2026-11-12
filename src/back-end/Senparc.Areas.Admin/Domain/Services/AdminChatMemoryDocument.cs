/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatMemoryDocument.cs
    文件功能描述：AdminChatMemoryDocument 相关定义与实现


    创建标识：Senparc - 20261103

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 完善聊天记忆的存储、检索与上下文处理

    修改标识：Senparc - 20261105
    修改描述：v0.12.26 整理记忆文档与证据内容

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Senparc.Ncf.Core.Exceptions;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record AdminChatMemoryEntry(
    string Key, string Text, string Topic, string Tier, bool Pinned, DateTimeOffset UpdatedAt)
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string WorkspaceId { get; init; } = "";
    public string Kind { get; init; } = "preference";
    public int Version { get; init; } = 1;
    public int? SourceMessageId { get; init; }
    public int? SourceSessionId { get; init; }
    public string Evidence { get; init; } = "";
    public string Source { get; init; } = "manual";
    public DateTimeOffset? ExpiresAt { get; init; }
}

public enum AdminChatMemoryMode { Off = 0, ReadOnly = 1, Learn = 2 }
public sealed record AdminChatMemoryRevision(string EntryId, int Version, string Action,
    string Source, DateTimeOffset At, AdminChatMemoryEntry Before, AdminChatMemoryEntry After);

public sealed class AdminChatMemoryProfile
{
    public string Revision { get; set; } = "";
    public string SystemMessage { get; set; } = "";
    public List<AdminChatMemoryEntry> Entries { get; set; } = new();
    public Dictionary<int, bool> Sessions { get; set; } = new();
    public Dictionary<int, AdminChatMemoryMode> SessionModes { get; set; } = new();
    public List<AdminChatMemoryRevision> History { get; set; } = new();

    public AdminChatMemoryMode ModeFor(int sessionId) => SessionModes.TryGetValue(sessionId, out var mode)
        ? mode : Sessions.GetValueOrDefault(sessionId) ? AdminChatMemoryMode.Learn : AdminChatMemoryMode.Off;

    public void Record(AdminChatMemoryEntry before, AdminChatMemoryEntry after, string source)
    {
        if (after == null)
        {
            History.RemoveAll(item => item.EntryId == before.Id);
            before = before with { Text = "", Evidence = "", Topic = "", SourceMessageId = null, SourceSessionId = null };
        }
        History.Add(new(after?.Id ?? before.Id, after?.Version ?? before.Version + 1,
            before == null ? "add" : after == null ? "delete" : "update", source, DateTimeOffset.UtcNow, before, after));
        if (History.Count > 100) History.RemoveRange(0, History.Count - 100);
    }
}

public sealed record AdminChatMemoryChange(
    string Key, string Text, string Topic, string Tier, string Evidence, bool Remove)
{
    public string Scope { get; init; } = "global";
    public string Kind { get; init; } = "preference";
}

public sealed record AdminChatMemoryMergeResult(bool Changed, string Reason, int GlobalChanges, int WorkspaceChanges)
{
    public bool RequiresReview { get; init; }
}

public sealed class AdminChatMemoryValidationException : NcfExceptionBase
{
    public string Reason { get; }
    public AdminChatMemoryValidationException(string reason) : base("Memory update rejected: " + reason) => Reason = reason;
}

public static class AdminChatMemoryDocument
{
    public const int MaxEntries = 80;
    public const int MaxMarkdownCharacters = 32000;
    // UTF-8 bytes are a conservative token bound for byte-based tokenizers, not measured usage.
    public const int ContextByteBudget = 2400;
    private static readonly JsonSerializerOptions ContextJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly Regex KeyPattern = new("^[a-z0-9-]{1,64}$", RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));
    private static readonly Regex UnsafeAutomaticText = new(
        @"https?://|[\w.+-]+@[\w.-]+\.\w+|-----BEGIN|(?:sk-|ghp_|github_pat_)[a-zA-Z0-9]|password|secret|api.?key|token\s*[:=]|密码|密钥|身份证|银行卡|住址|手机号|病史|诊断|宗教|政治|性取向",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static string Export(IEnumerable<AdminChatMemoryEntry> entries)
    {
        var all = entries.ToList();
        var result = new StringBuilder("# Memory\n\n## Core\n");
        foreach (var entry in all.Where(entry => entry.Tier == "core")) result.AppendLine("- " + entry.Text);
        foreach (var group in all.Where(entry => entry.Tier == "topic").GroupBy(entry => entry.Topic))
        {
            result.AppendLine("\n## Topic: " + group.Key);
            foreach (var entry in group) result.AppendLine("- " + entry.Text);
        }
        result.AppendLine("\n## Archive");
        foreach (var entry in all.Where(entry => entry.Tier == "archive")) result.AppendLine("- " + entry.Text);
        return result.ToString();
    }

    public static List<AdminChatMemoryEntry> Parse(string markdown)
    {
        if (markdown == null || markdown.Length > MaxMarkdownCharacters)
            throw new NcfExceptionBase("Memory.md exceeds 32000 characters.");
        var entries = new List<AdminChatMemoryEntry>();
        var tier = "core";
        var topic = "";
        foreach (var line in markdown.Replace("\r\n", "\n").Split('\n').Select(line => line.Trim()))
        {
            if (line.Length == 0 || line == "# Memory") continue;
            if (line is "## Core" or "## Archive")
            {
                tier = line == "## Core" ? "core" : "archive";
                topic = "";
                continue;
            }
            if (line.StartsWith("## Topic: ", StringComparison.Ordinal))
            {
                tier = "topic";
                topic = line[10..].Trim();
                if (topic.Length is < 1 or > 40) throw new NcfExceptionBase("Memory topic requires 1-40 characters.");
                continue;
            }
            if (!line.StartsWith("- ", StringComparison.Ordinal) || line.Length is < 3 or > 302)
                throw new NcfExceptionBase("Memory requires '- text' lines (1-300 characters) under Core, Topic or Archive headings.");
            entries.Add(new AdminChatMemoryEntry(Guid.NewGuid().ToString("N"), line[2..], topic, tier, true, DateTimeOffset.UtcNow));
        }
        if (entries.Count > MaxEntries) throw new NcfExceptionBase("Memory allows at most 80 entries.");
        if (entries.Select(entry => entry.Text).Distinct(StringComparer.Ordinal).Count() != entries.Count)
            throw new NcfExceptionBase("Duplicate Memory text. Keep one entry per fact.");
        return entries;
    }

    public static int Relevance(AdminChatMemoryEntry entry, string query)
    {
        var terms = Regex.Matches((query ?? "").ToLowerInvariant(), @"[\p{L}\p{N}]{2,}")
            .SelectMany(match => new[] { match.Value }.Concat(
                match.Value.Any(character => character is >= '\u3400' and <= '\u9fff')
                    ? Enumerable.Range(0, match.Value.Length - 1).Select(index => match.Value.Substring(index, 2))
                    : Array.Empty<string>()))
            .Distinct().Take(100).ToArray();
        return (entry.Topic.Length > 0
            && (query ?? "").Contains(entry.Topic, StringComparison.OrdinalIgnoreCase) ? 2 : 0) + terms.Count(term =>
            (entry.Topic + " " + entry.Text).Contains(term, StringComparison.OrdinalIgnoreCase)
            || term.Contains(entry.Topic, StringComparison.OrdinalIgnoreCase) && entry.Topic.Length > 0);
    }

    public static string BuildContext(IEnumerable<AdminChatMemoryEntry> entries, string query)
    {
        var result = new List<object>();
        var used = 2;
        var coreUsed = 0;
        var selected = entries.Where(entry => entry.Tier == "core" || entry.Tier == "topic" && Relevance(entry, query) > 0)
            .OrderBy(entry => entry.Tier == "core" ? 0 : 1)
            .ThenByDescending(entry => Relevance(entry, query)).ThenByDescending(entry => entry.Pinned)
            .ThenByDescending(entry => entry.UpdatedAt);
        foreach (var entry in selected)
        {
            var item = new { entry.Tier, entry.Topic, entry.Text };
            var bytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(item, ContextJson)) + 1;
            if (entry.Tier == "core" && coreUsed + bytes > ContextByteBudget / 2) continue;
            if (used + bytes > ContextByteBudget) continue;
            used += bytes;
            if (entry.Tier == "core") coreUsed += bytes;
            result.Add(item);
        }
        return result.Count == 0 ? "" : JsonSerializer.Serialize(result, ContextJson);
    }

    public static (string Intent, string Changes) ReadExtraction(string json)
    {
        if (json == null || json.Length > 12000)
            throw new AdminChatMemoryValidationException("invalid-change");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2
            || !root.TryGetProperty("intent", out var intent) || intent.ValueKind != JsonValueKind.String
            || !root.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array
            || intent.GetString() is not ("update" or "defer" or "none")
            || intent.GetString() != "update" && changes.GetArrayLength() != 0)
            throw new AdminChatMemoryValidationException("invalid-change");
        return (intent.GetString(), changes.GetRawText());
    }

    public static bool Merge(AdminChatMemoryProfile profile, string json, string userMessage,
        string workspaceId = "", int? sourceMessageId = null, int? sourceSessionId = null) =>
        MergeWithResult(profile, json, userMessage, workspaceId, sourceMessageId, sourceSessionId).Changed;

    public static AdminChatMemoryMergeResult MergeWithResult(AdminChatMemoryProfile profile, string json, string userMessage,
        string workspaceId = "", int? sourceMessageId = null, int? sourceSessionId = null,
        IReadOnlyList<AdminChatMemorySource> recentMessages = null)
    {
        if (json == null || json.Length > 12000) throw new NcfExceptionBase("Memory updater returned oversized output.");
        var changes = JsonSerializer.Deserialize<List<AdminChatMemoryChange>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow })
            ?? throw new NcfExceptionBase("Memory updater returned null.");
        if (changes.Count > 5) throw new NcfExceptionBase("Memory updater returned too many changes.");
        AdminChatMemorySource EvidenceSource(string evidence) =>
            string.IsNullOrWhiteSpace(evidence) ? null
            : (userMessage ?? "").Contains(evidence, StringComparison.Ordinal)
                ? new(sourceMessageId ?? 0, "user", userMessage)
                : recentMessages?.Where(source => source.Role is "user" or "assistant"
                        && source.Text.Contains(evidence, StringComparison.Ordinal))
                    .OrderBy(source => source.Role == "user" ? 0 : 1)
                    .ThenByDescending(source => source.MessageId).FirstOrDefault();
        foreach (var change in changes)
        {
            if (change == null || change.Key == null || !KeyPattern.IsMatch(change.Key)
                || string.IsNullOrWhiteSpace(change.Evidence) || change.Evidence.Length > 600
                || EvidenceSource(change.Evidence) == null
                || change.Tier is not ("core" or "topic")
                || string.IsNullOrWhiteSpace(change.Text) || change.Text.Length > 300
                || change.Topic == null || change.Topic.Length > 40 || change.Tier == "topic" && change.Topic.Length == 0
                || change.Text.Any(char.IsControl) || change.Topic.Any(char.IsControl)
                || change.Scope is not ("global" or "workspace")
                || change.Scope == "workspace" && string.IsNullOrEmpty(workspaceId)
                || change.Kind is not ("preference" or "decision" or "project" or "reference")
                || change.Kind is not ("preference" or "reference") && change.Scope != "workspace"
                || change.Remove && !Regex.IsMatch(userMessage ?? "", @"忘记|删除|不要再|forget|remove|delete",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))
                || UnsafeAutomaticText.IsMatch(change.Text + " " + change.Evidence))
                throw new AdminChatMemoryValidationException("invalid-change");
        }
        var updated = profile.Entries.ToList();
        var revisions = new List<(AdminChatMemoryEntry Before, AdminChatMemoryEntry After)>();
        var pinned = false;
        var requiresReview = false;
        foreach (var change in changes)
        {
            var scope = change.Scope == "workspace" ? workspaceId : "";
            var previous = updated.Find(entry => entry.Key == change.Key && entry.WorkspaceId == scope);
            if (previous?.Pinned == true || updated.Any(entry => entry.Pinned && entry.Text == change.Text && entry.WorkspaceId == scope))
            {
                pinned = true;
                continue;
            }
            if (!change.Remove && updated.Any(entry =>
                entry.Text == change.Text && entry.Topic == change.Topic && entry.Tier == change.Tier && entry.WorkspaceId == scope)) continue;
            var source = EvidenceSource(change.Evidence);
            if (previous != null) updated.Remove(previous);
            AdminChatMemoryEntry next = null;
            if (!change.Remove)
            {
                next = new AdminChatMemoryEntry(change.Key, change.Text, change.Topic, change.Tier, false, DateTimeOffset.UtcNow)
                {
                    Id = previous?.Id ?? Guid.NewGuid().ToString("N"), Version = (previous?.Version ?? 0) + 1,
                    WorkspaceId = scope, Kind = change.Kind, Source = source.Role + "-message",
                    SourceMessageId = source.MessageId > 0 ? source.MessageId : null,
                    SourceSessionId = sourceSessionId, Evidence = change.Evidence
                };
                updated.Add(next);
            }
            if (previous != null || next != null)
            {
                revisions.Add((previous, next));
                requiresReview |= source.Role == "assistant";
            }
        }
        if (updated.Count > MaxEntries) throw new AdminChatMemoryValidationException("capacity");
        if (updated.SequenceEqual(profile.Entries))
            return new(false, changes.Count == 0 ? "no-candidates" : pinned ? "pinned" : "no-difference", 0, 0);
        profile.Entries = updated;
        foreach (var revision in revisions) profile.Record(revision.Before, revision.After, "automatic");
        return new(true, "saved", revisions.Count(item => (item.After ?? item.Before).WorkspaceId.Length == 0),
            revisions.Count(item => (item.After ?? item.Before).WorkspaceId.Length > 0))
        { RequiresReview = requiresReview };
    }
}
