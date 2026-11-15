/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatSessionService.cs
    文件功能描述：AdminChatSessionService.cs implementation and project behavior.


    创建标识：Senparc - 20260325

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    修改标识：Senparc - 20261021
    修改描述：v0.11.1 保存会话工作区与标题

    修改标识：Senparc - 20261103
    修改描述：验证分类和所有权并原子保存会话排序

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 完善后台聊天会话管理与排序

    修改标识：Senparc - 20261106
    修改描述：归档会话只从进行中列表移出，并支持单独查询

    修改标识：Senparc - 20261106
    修改描述：按账号持久化会话未读提醒

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.Core.Exceptions;
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
        /// 获取用户已归档、尚未删除的会话。
        /// </summary>
        public async Task<(List<AdminChatSession> sessions, int totalCount)> GetUserArchivedSessionsAsync(int userId, int pageIndex = 1, int pageSize = 20)
        {
            var result = await base.GetObjectListAsync(
                pageIndex,
                pageSize,
                s => s.UserId == userId && s.Status == ChatSessionStatus.Archived,
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

        public async Task<List<AdminChatSession>> ReorderSessionAsync(
            int userId, int sessionId, int targetSessionId, bool insertAfter)
        {
            if (userId <= 0 || sessionId <= 0 || targetSessionId <= 0 || sessionId == targetSessionId)
                throw new NcfExceptionBase("Invalid session order request.");

            var source = await GetSessionByIdAsync(sessionId, userId);
            var target = await GetSessionByIdAsync(targetSessionId, userId);
            if (source == null || target == null || source.Status != ChatSessionStatus.Active
                || target.Status != ChatSessionStatus.Active
                || source.TenantId != target.TenantId
                || (source.WorkspaceId ?? "") != (target.WorkspaceId ?? ""))
                throw new NcfExceptionBase("Only your active sessions in the same Workspace can be reordered.");

            var db = BaseData.BaseDB.BaseDataContext;
            var sessions = await db.Set<AdminChatSession>()
                .Where(s => s.TenantId == source.TenantId && s.UserId == userId && !s.Flag
                    && s.Status == ChatSessionStatus.Active && s.WorkspaceId == source.WorkspaceId)
                .OrderBy(s => s.SortOrder ?? int.MaxValue)
                .ThenByDescending(s => s.LastMessageTime)
                .ThenByDescending(s => s.Id)
                .ToListAsync();
            var moved = sessions.Single(s => s.Id == sessionId);
            sessions.Remove(moved);
            sessions.Insert(sessions.FindIndex(s => s.Id == targetSessionId) + (insertAfter ? 1 : 0), moved);
            for (var index = 0; index < sessions.Count; index++)
            {
                sessions[index].SetSortOrder(index);
                // Concurrent reorder requests must replace the whole order, not merge changed ranks.
                db.Entry(sessions[index]).Property(s => s.SortOrder).IsModified = true;
            }
            await db.SaveChangesAsync();
            return sessions;
        }

        /// <summary>
        /// 创建新的聊天会话
        /// </summary>
        public async Task<AdminChatSession> CreateSessionAsync(
            string title,
            int userId,
            string workspaceId = null,
            bool workspaceWriteEnabled = false,
            bool workspaceCliEnabled = false)
        {
            var session = new AdminChatSession(
                title,
                userId,
                workspaceId,
                workspaceWriteEnabled,
                workspaceCliEnabled);
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
            if (session == null || session.Status == ChatSessionStatus.Deleted) return false;
            if (session.Status == ChatSessionStatus.Archived) return true;

            session.Archive();
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        /// 保存会话未读状态。只允许会话所属用户修改，已删除会话不再提醒。
        /// </summary>
        public async Task<bool> SetSessionUnreadAsync(int sessionId, int userId, bool unread)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null || session.Status == ChatSessionStatus.Deleted) return false;
            if (session.HasUnread == unread) return true;

            session.SetUnread(unread);
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
        /// 将已归档会话还原为进行中。已删除会话不能还原。
        /// </summary>
        public async Task<bool> RestoreSessionAsync(int sessionId, int userId)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null || session.Status == ChatSessionStatus.Deleted) return false;
            if (session.Status == ChatSessionStatus.Active) return true;

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

        /// <summary>
        /// 按用户统计会话数量（管理端监控用，仅返回数量与最后活跃时间，不包含任何会话/消息内容）
        /// </summary>
        public async Task<List<AdminChatUserSessionStat>> GetUserSessionStatsAsync()
        {
            var db = base.BaseData.BaseDB.BaseDataContext;
            var stats = await db.Set<AdminChatSession>()
                .AsNoTracking()
                .GroupBy(s => s.UserId)
                .Select(g => new AdminChatUserSessionStat
                {
                    UserId = g.Key,
                    TotalSessionCount = g.Count(),
                    ActiveSessionCount = g.Count(s => s.Status == ChatSessionStatus.Active),
                    ArchivedSessionCount = g.Count(s => s.Status == ChatSessionStatus.Archived),
                    DeletedSessionCount = g.Count(s => s.Status == ChatSessionStatus.Deleted),
                    LastActiveTime = g.Max(s => s.LastMessageTime)
                })
                .ToListAsync();
            return stats;
        }
    }

    /// <summary>
    /// 单个用户的 AdminChat 会话统计（管理端监控用，仅含数量，不含内容）
    /// </summary>
    public class AdminChatUserSessionStat
    {
        public int UserId { get; set; }
        public int TotalSessionCount { get; set; }
        public int ActiveSessionCount { get; set; }
        public int ArchivedSessionCount { get; set; }
        public int DeletedSessionCount { get; set; }
        public DateTime LastActiveTime { get; set; }
    }
}
