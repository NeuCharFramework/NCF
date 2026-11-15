/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatMessageService.cs
    文件功能描述：AdminChatMessageService 相关功能实现


    创建标识：Senparc - 20260325

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

    修改标识：Senparc - 20261105
    修改描述：v0.12.26 持久化消息、Prompt 引用与评分

    修改标识：Senparc - 20261110
    修改描述：按当前用户检索未归档会话中的消息片段

    修改标识：Senparc - 20261110
    修改描述：检索未归档会话中 Harness 实际调用过的方法名

    修改标识：Senparc - 20261111
    修改描述：v0.12.28 完善 Admin Chat 与模块工作区交互能力兼容

----------------------------------------------------------------*/
using Microsoft.EntityFrameworkCore;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.PromptRange.Abstractions.References;
using Senparc.Ncf.Core.Exceptions;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// AdminChatMessageService：管理后台聊天消息服务
    /// </summary>
    public class AdminChatMessageService : BaseClientService<AdminChatMessage>
    {
        public AdminChatMessageService(IAdminChatMessageRepository repository, IServiceProvider serviceProvider) 
            : base(repository, serviceProvider)
        {
        }

        /// <summary>
        /// 获取会话的所有消息（按序号正序）
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="pageIndex">页码（从1开始，0表示获取全部）</param>
        /// <param name="pageSize">每页数量</param>
        public async Task<(List<AdminChatMessage> messages, int totalCount)> GetSessionMessagesAsync(int sessionId, int pageIndex = 0, int pageSize = 50)
        {
            if (pageIndex == 0)
            {
                var allMessages = await base.GetFullListAsync(
                    m => m.SessionId == sessionId, 
                    "Sequence ASC");
                return (allMessages.ToList(), allMessages.Count());
            }
            else
            {
                var result = await base.GetObjectListAsync(
                    pageIndex, 
                    pageSize, 
                    m => m.SessionId == sessionId, 
                    m => m.Sequence, 
                    Ncf.Core.Enums.OrderingType.Ascending);
                return (result.ToList(), result.TotalCount);
            }
        }

        /// <summary>
        /// 获取下一个序号（用于新消息）
        /// </summary>
        public async Task<int> GetNextSequenceAsync(int sessionId)
        {
            var messages = await base.GetFullListAsync(m => m.SessionId == sessionId);
            var maxSequence = messages.Any() ? messages.Max(m => m.Sequence) : 0;
            return maxSequence + 1;
        }

        /// <summary>
        /// 添加新消息
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="roleType">角色类型</param>
        /// <param name="content">消息内容</param>
        /// <param name="modelIdentifier">模型标识符（可选）</param>
        public async Task<AdminChatMessage> AddMessageAsync(
            int sessionId,
            ChatMessageRoleType roleType,
            string content,
            string modelIdentifier = null,
            int? trajectoryId = null,
            int? trajectorySequence = null,
            IReadOnlyList<AdminChatAttachment> attachments = null,
            IReadOnlyList<PromptReference> promptReferences = null)
        {
            if (promptReferences?.Count > 0)
            {
                if (roleType != ChatMessageRoleType.User)
                    throw new ArgumentException("Only user messages may declare prompt references.");
                var references = _serviceProvider.GetService<IPromptReferenceService>()
                    ?? throw new InvalidOperationException("PromptRange is unavailable. Remove prompt references to send plain text.");
                await references.ValidateAsync(content, promptReferences);
            }
            var sequence = await GetNextSequenceAsync(sessionId);
            var message = new AdminChatMessage(
                sessionId,
                roleType,
                content,
                sequence,
                modelIdentifier,
                trajectoryId,
                trajectorySequence);
            message.SetAttachments(attachments);
            message.SetPromptReferences(promptReferences);
            await base.SaveObjectAsync(message);
            return message;
        }

        public async Task<bool> SetTrajectoryAsync(int messageId, int? trajectoryId, int? trajectorySequence)
        {
            var message = await base.GetObjectAsync(m => m.Id == messageId);
            if (message == null)
            {
                return false;
            }

            message.SetTrajectory(trajectoryId, trajectorySequence);
            await base.SaveObjectAsync(message);
            return true;
        }

        public async Task<PromptReferenceInfo> ScorePromptReferenceWithAIAsync(int userId, int messageId, int promptItemId)
        {
            if (userId <= 0) throw new NcfExceptionBase("Sign in to score a prompt.");
            var message = await GetObjectAsync(item => item.Id == messageId)
                ?? throw new NcfExceptionBase("Message not found.");
            var session = await _serviceProvider.GetRequiredService<AdminChatSessionService>()
                .GetSessionByIdAsync(message.SessionId, userId);
            if (session == null || message.RoleType != ChatMessageRoleType.User ||
                !message.GetPromptReferences().Any(reference => reference.PromptItemId == promptItemId))
                throw new NcfExceptionBase("Message or prompt reference is not accessible.");
            var following = await GetObjectListAsync(1, 1,
                item => item.SessionId == message.SessionId && item.Sequence > message.Sequence,
                item => item.Sequence, Ncf.Core.Enums.OrderingType.Ascending);
            var result = following.FirstOrDefault();
            if (result?.RoleType != ChatMessageRoleType.Assistant || string.IsNullOrWhiteSpace(result.Content))
                throw new NcfExceptionBase("This prompt reference has no completed reply to score.");
            var provider = _serviceProvider.GetService<IPromptReferenceService>()
                ?? throw new NcfExceptionBase("PromptRange is unavailable.");
            return await provider.ScoreWithAIAsync(promptItemId, message.Content, result.Content);
        }

        /// <summary>
        /// 设置消息反馈
        /// </summary>
        public async Task<bool> SetMessageFeedbackAsync(int messageId, MessageFeedbackType feedback)
        {
            var message = await base.GetObjectAsync(m => m.Id == messageId);
            if (message == null) return false;

            message.SetFeedback(feedback);
            await base.SaveObjectAsync(message);
            return true;
        }

        /// <summary>
        /// 更新消息内容（用于流式输出场景）
        /// </summary>
        public async Task<bool> UpdateMessageContentAsync(int messageId, string newContent)
        {
            var message = await base.GetObjectAsync(m => m.Id == messageId);
            if (message == null) return false;

            message.UpdateContent(newContent);
            await base.SaveObjectAsync(message);
            return true;
        }

        /// <summary>
        /// 删除会话的所有消息（物理删除，慎用）
        /// </summary>
        public async Task<int> DeleteSessionMessagesAsync(int sessionId)
        {
            var messages = await base.GetFullListAsync(m => m.SessionId == sessionId);

            foreach (var message in messages)
            {
                await base.DeleteObjectAsync(message);
            }

            return messages.Count();
        }

        /// <summary>
        /// 按消息ID批量删除会话消息（物理删除）
        /// </summary>
        public async Task<int> DeleteMessagesAsync(int sessionId, IEnumerable<int> messageIds)
        {
            var ids = (messageIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (!ids.Any())
            {
                return 0;
            }

            var messages = await base.GetFullListAsync(m => m.SessionId == sessionId && ids.Contains(m.Id));
            var deletedCount = 0;

            foreach (var message in messages)
            {
                await base.DeleteObjectAsync(message);
                deletedCount++;
            }

            return deletedCount;
        }

        /// <summary>
        /// 获取会话的最后一条消息
        /// </summary>
        public async Task<AdminChatMessage> GetLastMessageAsync(int sessionId)
        {
            var messages = await base.GetFullListAsync(m => m.SessionId == sessionId, "Sequence DESC");
            return messages.FirstOrDefault();
        }

        /// <summary>
        /// 获取会话的消息数量
        /// </summary>
        public async Task<int> GetMessageCountAsync(int sessionId)
        {
            var messages = await base.GetFullListAsync(m => m.SessionId == sessionId);
            return messages.Count();
        }

        /// <summary>
        /// 获取归属校验通过的消息：仅当消息所属会话属于指定用户时返回，否则返回 null。
        /// 用于消息级操作（如反馈）的防御性越权防护。
        /// </summary>
        public async Task<AdminChatMessage> GetOwnedMessageAsync(int messageId, int userId)
        {
            if (messageId <= 0 || userId <= 0)
            {
                return null;
            }

            return await base.GetObjectAsync(m => m.Id == messageId && m.Session.UserId == userId);
        }

        /// <summary>
        /// 设置消息反馈（带会话归属校验，防止跨账号越权操作）
        /// </summary>
        public async Task<bool> SetMessageFeedbackAsync(int messageId, int userId, MessageFeedbackType feedback)
        {
            var message = await GetOwnedMessageAsync(messageId, userId);
            if (message == null) return false;

            message.SetFeedback(feedback);
            await base.SaveObjectAsync(message);
            return true;
        }

        /// <summary>
        /// 在当前用户未归档、未删除的会话里检索消息。已归档会话的正文不会被读出。
        /// </summary>
        public async Task<IReadOnlyList<AdminChatHistorySearchHit>> SearchActiveHistoryAsync(
            int userId,
            string keyword,
            int limit = AdminChatHistorySearch.DefaultLimit)
        {
            if (userId <= 0 || AdminChatHistorySearch.NormalizeKeyword(keyword).Length == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var db = BaseData.BaseDB.BaseDataContext;
            var sessions = await db.Set<AdminChatSession>()
                .AsNoTracking()
                .Where(session => session.UserId == userId && session.Status == ChatSessionStatus.Active)
                .ToListAsync();
            if (sessions.Count == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var sessionIds = sessions.Select(session => session.Id).ToList();
            var messages = await db.Set<AdminChatMessage>()
                .AsNoTracking()
                .Where(message => sessionIds.Contains(message.SessionId))
                .ToListAsync();
            return AdminChatHistorySearch.Search(sessions, messages, userId, keyword, limit);
        }

        /// <summary>
        /// 在当前用户未归档会话的 Harness 轨迹中检索 tool.call 方法名。
        /// </summary>
        public async Task<IReadOnlyList<AdminChatHistorySearchHit>> SearchActiveToolCallsAsync(
            int userId,
            string keyword,
            int limit = AdminChatHistorySearch.DefaultLimit)
        {
            if (userId <= 0 || AdminChatHistorySearch.NormalizeKeyword(keyword).Length == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var db = BaseData.BaseDB.BaseDataContext;
            var sessions = await db.Set<AdminChatSession>()
                .AsNoTracking()
                .Where(session => session.UserId == userId && session.Status == ChatSessionStatus.Active)
                .ToListAsync();
            if (sessions.Count == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var sessionIds = sessions.Select(session => session.Id).ToList();
            var trajectories = await db.Set<AdminChatTrajectory>()
                .AsNoTracking()
                .Where(trajectory => trajectory.UserId == userId && sessionIds.Contains(trajectory.SessionId))
                .ToListAsync();
            if (trajectories.Count == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var trajectoryIds = trajectories.Select(trajectory => trajectory.Id).ToList();
            var events = await db.Set<AdminChatTrajectoryEvent>()
                .AsNoTracking()
                .Where(item => trajectoryIds.Contains(item.TrajectoryId) && item.EventType == "tool.call")
                .ToListAsync();
            var messages = await db.Set<AdminChatMessage>()
                .AsNoTracking()
                .Where(message => sessionIds.Contains(message.SessionId) && message.TrajectoryId != null)
                .ToListAsync();
            return AdminChatHistorySearch.SearchToolCalls(sessions, trajectories, events, messages, userId, keyword, limit);
        }

        /// <summary>
        /// 按用户统计消息数量（管理端监控用，仅返回数量，不包含内容）
        /// </summary>
        public async Task<List<(int UserId, int Count)>> GetMessageCountByUserAsync()
        {
            var db = base.BaseData.BaseDB.BaseDataContext;
            var perSession = await db.Set<AdminChatMessage>()
                .AsNoTracking()
                .GroupBy(m => m.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToListAsync();

            var sessions = await db.Set<AdminChatSession>()
                .AsNoTracking()
                .Select(s => new { s.Id, s.UserId })
                .ToListAsync();

            var perSessionMap = perSession.ToDictionary(x => x.SessionId, x => x.Count);
            var result = new Dictionary<int, int>();
            foreach (var session in sessions)
            {
                if (perSessionMap.TryGetValue(session.Id, out var count))
                {
                    result[session.UserId] = (result.TryGetValue(session.UserId, out var existing) ? existing : 0) + count;
                }
            }
            return result.Select(kv => (kv.Key, kv.Value)).ToList();
        }
    }
}
