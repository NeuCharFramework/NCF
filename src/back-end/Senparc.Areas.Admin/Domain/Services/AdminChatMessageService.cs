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
        public async Task<AdminChatMessage> AddMessageAsync(int sessionId, ChatMessageRoleType roleType, string content, string modelIdentifier = null)
        {
            var sequence = await GetNextSequenceAsync(sessionId);
            var message = new AdminChatMessage(sessionId, roleType, content, sequence, modelIdentifier);
            await base.SaveObjectAsync(message);
            return message;
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
    }
}
