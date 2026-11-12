/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatHistorySearch.cs
    文件功能描述：在未归档的 Admin Chat 会话中检索消息片段


    创建标识：Senparc - 20261110

    修改标识：Senparc - 20261110
    修改描述：提供未归档对话的关键字检索与高亮片段

    修改标识：Senparc - 20261110
    修改描述：增加未归档会话中 Harness 方法调用名检索

    修改标识：Senparc - 20261111
    修改描述：v0.12.28 完善 Admin Chat 与模块工作区交互能力兼容

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;

namespace Senparc.Areas.Admin.Domain.Services
{
    public enum AdminChatHistorySearchMatchKind
    {
        Content = 0,
        Title = 1,
        ToolCall = 2
    }

    /// <summary>
    /// 一条可定位到会话和消息的搜索结果。片段只保留命中附近的文本。
    /// </summary>
    public sealed class AdminChatHistorySearchHit
    {
        public int SessionId { get; set; }
        public string Title { get; set; }
        public int MessageId { get; set; }
        public ChatMessageRoleType RoleType { get; set; }
        public AdminChatHistorySearchMatchKind MatchKind { get; set; }
        public DateTime OccurredAt { get; set; }
        public string SnippetBefore { get; set; }
        public string SnippetMatch { get; set; }
        public string SnippetAfter { get; set; }
        public bool TitleMatched { get; set; }
        public int TrajectoryId { get; set; }
        public int EventSequence { get; set; }
    }

    /// <summary>
    /// 只匹配当前用户仍在进行中的会话。已归档、已删除和其他账号的记录不会进入结果。
    /// </summary>
    public static class AdminChatHistorySearch
    {
        public const int DefaultLimit = 10;
        public const int MaxKeywordLength = 80;
        private const int SnippetRadius = 42;

        public static string NormalizeKeyword(string keyword)
        {
            var normalized = (keyword ?? string.Empty).Trim();
            return normalized.Length <= MaxKeywordLength
                ? normalized
                : normalized.Substring(0, MaxKeywordLength);
        }

        public static IReadOnlyList<AdminChatHistorySearchHit> Search(
            IEnumerable<AdminChatSession> sessions,
            IEnumerable<AdminChatMessage> messages,
            int userId,
            string keyword,
            int limit = DefaultLimit)
        {
            var normalized = NormalizeKeyword(keyword);
            var take = limit <= 0 ? 0 : Math.Min(limit, DefaultLimit);
            if (userId <= 0 || normalized.Length == 0 || take == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var activeSessions = SelectActiveSessions(sessions, userId);
            if (activeSessions.Count == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var hits = new List<AdminChatHistorySearchHit>();
            var sessionsWithContent = new HashSet<int>();
            foreach (var message in messages ?? Array.Empty<AdminChatMessage>())
            {
                if (message == null || message.Flag || string.IsNullOrEmpty(message.Content)
                    || !activeSessions.TryGetValue(message.SessionId, out var session)
                    || message.Content.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                SplitSnippet(message.Content, normalized, out var before, out var match, out var after);
                sessionsWithContent.Add(session.Id);
                hits.Add(new AdminChatHistorySearchHit
                {
                    SessionId = session.Id,
                    Title = session.Title,
                    MessageId = message.Id,
                    RoleType = message.RoleType,
                    MatchKind = AdminChatHistorySearchMatchKind.Content,
                    OccurredAt = message.AddTime,
                    SnippetBefore = before,
                    SnippetMatch = match,
                    SnippetAfter = after,
                    TitleMatched = session.Title?.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0
                });
            }

            foreach (var session in activeSessions.Values)
            {
                if (sessionsWithContent.Contains(session.Id)
                    || string.IsNullOrEmpty(session.Title)
                    || session.Title.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                SplitSnippet(session.Title, normalized, out var before, out var match, out var after);
                hits.Add(new AdminChatHistorySearchHit
                {
                    SessionId = session.Id,
                    Title = session.Title,
                    MessageId = 0,
                    MatchKind = AdminChatHistorySearchMatchKind.Title,
                    OccurredAt = session.LastMessageTime,
                    SnippetBefore = before,
                    SnippetMatch = match,
                    SnippetAfter = after,
                    TitleMatched = true
                });
            }

            return hits
                .OrderByDescending(hit => hit.OccurredAt)
                .ThenByDescending(hit => hit.MessageId)
                .Take(take)
                .ToList();
        }

        /// <summary>
        /// 只匹配 Harness 轨迹中 EventType 为 tool.call 的方法名。参数、结果和普通消息不参与。
        /// </summary>
        public static IReadOnlyList<AdminChatHistorySearchHit> SearchToolCalls(
            IEnumerable<AdminChatSession> sessions,
            IEnumerable<AdminChatTrajectory> trajectories,
            IEnumerable<AdminChatTrajectoryEvent> events,
            IEnumerable<AdminChatMessage> messages,
            int userId,
            string keyword,
            int limit = DefaultLimit)
        {
            var normalized = NormalizeKeyword(keyword);
            var take = limit <= 0 ? 0 : Math.Min(limit, DefaultLimit);
            var activeSessions = SelectActiveSessions(sessions, userId);
            if (userId <= 0 || normalized.Length == 0 || take == 0 || activeSessions.Count == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var activeTrajectories = (trajectories ?? Array.Empty<AdminChatTrajectory>())
                .Where(trajectory => trajectory != null
                    && !trajectory.Flag
                    && trajectory.UserId == userId
                    && activeSessions.ContainsKey(trajectory.SessionId))
                .GroupBy(trajectory => trajectory.Id)
                .Select(group => group.First())
                .ToDictionary(trajectory => trajectory.Id);
            if (activeTrajectories.Count == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var hits = new List<AdminChatHistorySearchHit>();
            foreach (var item in events ?? Array.Empty<AdminChatTrajectoryEvent>())
            {
                if (item == null || item.Flag
                    || !string.Equals(item.EventType, "tool.call", StringComparison.Ordinal)
                    || string.IsNullOrEmpty(item.Name)
                    || item.Name.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) < 0
                    || !activeTrajectories.TryGetValue(item.TrajectoryId, out var trajectory))
                {
                    continue;
                }

                var session = activeSessions[trajectory.SessionId];
                SplitSnippet(item.Name, normalized, out var before, out var match, out var after);
                hits.Add(new AdminChatHistorySearchHit
                {
                    SessionId = session.Id,
                    Title = session.Title,
                    MessageId = LocateToolCallMessage(messages, item.TrajectoryId, item.Sequence),
                    MatchKind = AdminChatHistorySearchMatchKind.ToolCall,
                    OccurredAt = item.OccurredAt,
                    SnippetBefore = before,
                    SnippetMatch = match,
                    SnippetAfter = after,
                    TrajectoryId = item.TrajectoryId,
                    EventSequence = item.Sequence
                });
            }

            return hits
                .OrderByDescending(hit => hit.OccurredAt)
                .ThenByDescending(hit => hit.EventSequence)
                .Take(take)
                .ToList();
        }

        private static Dictionary<int, AdminChatSession> SelectActiveSessions(
            IEnumerable<AdminChatSession> sessions,
            int userId)
        {
            return (sessions ?? Array.Empty<AdminChatSession>())
                .Where(session => session != null
                    && session.UserId == userId
                    && !session.Flag
                    && session.Status == ChatSessionStatus.Active)
                .GroupBy(session => session.Id)
                .Select(group => group.First())
                .ToDictionary(session => session.Id);
        }

        private static int LocateToolCallMessage(
            IEnumerable<AdminChatMessage> messages,
            int trajectoryId,
            int sequence)
        {
            var related = (messages ?? Array.Empty<AdminChatMessage>())
                .Where(message => message != null && !message.Flag && message.TrajectoryId == trajectoryId)
                .ToList();
            var covering = related
                .Where(message => message.TrajectorySequence >= sequence)
                .OrderBy(message => message.TrajectorySequence)
                .ThenBy(message => message.Id)
                .FirstOrDefault();
            if (covering != null)
            {
                return covering.Id;
            }

            return related
                .OrderByDescending(message => message.TrajectorySequence)
                .ThenByDescending(message => message.Id)
                .FirstOrDefault()?.Id ?? 0;
        }

        private static void SplitSnippet(string text, string keyword, out string before, out string match, out string after)
        {
            text ??= string.Empty;
            var index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                before = Collapse(text.Length > SnippetRadius * 2 ? text.Substring(0, SnippetRadius * 2) : text).Trim();
                match = string.Empty;
                after = text.Length > SnippetRadius * 2 ? "…" : string.Empty;
                return;
            }

            var matchEnd = index + keyword.Length;
            var start = Math.Max(0, index - SnippetRadius);
            var end = Math.Min(text.Length, matchEnd + SnippetRadius);
            before = Collapse(text.Substring(start, index - start));
            match = Collapse(text.Substring(index, keyword.Length));
            after = Collapse(text.Substring(matchEnd, end - matchEnd));
            if (start > 0)
            {
                before = "…" + before.TrimStart();
            }
            else
            {
                before = before.TrimStart();
            }

            after = end < text.Length ? after.TrimEnd() + "…" : after.TrimEnd();
        }

        private static string Collapse(string value) =>
            string.IsNullOrEmpty(value) ? string.Empty : Regex.Replace(value, @"\s+", " ");
    }
}
