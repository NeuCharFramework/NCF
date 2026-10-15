/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatMessageService.cs
    文件功能描述：AdminChatMessageService 相关功能实现


    创建标识：Senparc - 20260325

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

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
            int? trajectorySequence = null)
        {
            var sequence = await GetNextSequenceAsync(sessionId);
            var message = new AdminChatMessage(
                sessionId,
                roleType,
                content,
                sequence,
                modelIdentifier,
                trajectoryId,
                trajectorySequence);
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
