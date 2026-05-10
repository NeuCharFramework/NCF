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
    /// AdminChatSessionService：管理后台聊天会话服务
    /// </summary>
    public class AdminChatSessionService : BaseClientService<AdminChatSession>
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public AdminChatSessionService(IAdminChatSessionRepository repository, IServiceProvider serviceProvider) 
            : base(repository, serviceProvider)
        {
        }

        /// <summary>
        /// 获取用户的活跃会话列表（按最后消息时间倒序）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pageIndex">页码（从1开始）</param>
        /// <param name="pageSize">每页数量</param>
        public async Task<(List<AdminChatSession> sessions, int totalCount)> GetUserActiveSessionsAsync(int userId, int pageIndex = 1, int pageSize = 20)
        {
            var result = await base.GetObjectListAsync(
                pageIndex, 
                pageSize, 
                s => s.UserId == userId && s.Status == ChatSessionStatus.Active, 
                s => s.LastMessageTime, 
                Ncf.Core.Enums.OrderingType.Descending);

            return (result.ToList(), result.TotalCount);
        }

        /// <summary>
        /// 根据 ID 获取会话（包含验证用户权限）
        /// </summary>
        public async Task<AdminChatSession> GetSessionByIdAsync(int sessionId, int userId)
        {
            var session = await base.GetObjectAsync(s => s.Id == sessionId && s.UserId == userId);
            return session;
        }

        /// <summary>
        /// 创建新的聊天会话
        /// </summary>
        public async Task<AdminChatSession> CreateSessionAsync(string title, int userId)
        {
            var session = new AdminChatSession(title, userId);
            await base.SaveObjectAsync(session);
            return session;
        }

        /// <summary>
        /// 更新会话标题
        /// </summary>
        public async Task<bool> UpdateSessionTitleAsync(int sessionId, int userId, string newTitle)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null) return false;

            session.UpdateTitle(newTitle);
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        /// 更新会话的最后消息时间
        /// </summary>
        public async Task UpdateLastMessageTimeAsync(int sessionId)
        {
            var session = await base.GetObjectAsync(s => s.Id == sessionId);
            if (session != null)
            {
                session.UpdateLastMessageTime();
                await base.SaveObjectAsync(session);
            }
        }

        /// <summary>
        /// 归档会话
        /// </summary>
        public async Task<bool> ArchiveSessionAsync(int sessionId, int userId)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null) return false;

            session.Archive();
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        /// 删除会话（软删除）
        /// </summary>
        public async Task<bool> DeleteSessionAsync(int sessionId, int userId)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null) return false;

            session.Delete();
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        /// 恢复已删除的会话
        /// </summary>
        public async Task<bool> RestoreSessionAsync(int sessionId, int userId)
        {
            var session = await base.GetObjectAsync(s => s.Id == sessionId && s.UserId == userId);
            if (session == null) return false;

            session.Restore();
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        /// 获取会话总数（按状态）
        /// </summary>
        public async Task<int> GetSessionCountByStatusAsync(int userId, ChatSessionStatus status)
        {
            var sessions = await base.GetFullListAsync(s => s.UserId == userId && s.Status == status);
            return sessions.Count();
        }
    }
}
