/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminChatSession.cs
    文件功能描述：后台对话会话
    
    
    创建标识：Senparc - 20260325
    
    修改标识：Senparc - 20261021
    修改描述：v0.11.1 会话关联工作区

    修改标识：Senparc - 20261103
    修改描述：保存分类内会话排序

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 完善后台聊天会话管理与排序

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    /// AdminChatSession：管理后台聊天会话
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatSession))] //必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class AdminChatSession : EntityBase<int>
    {
        /// <summary>
        /// 会话标题（从首条消息自动提取，最多150字符）
        /// </summary>
        [Required, MaxLength(150)]
        public string Title { get; private set; }

        /// <summary>
        /// 用户ID（外键到 AdminUserInfo）
        /// </summary>
        [Required]
        public int UserId { get; private set; }

        /// <summary>
        /// 会话状态
        /// </summary>
        [Required]
        public ChatSessionStatus Status { get; private set; }

        /// <summary>
        /// 最后一条消息时间
        /// </summary>
        public DateTime LastMessageTime { get; private set; }

        [MaxLength(64)]
        public string WorkspaceId { get; private set; }

        public bool WorkspaceWriteEnabled { get; private set; }

        public bool WorkspaceCliEnabled { get; private set; }

        public int? SortOrder { get; private set; }

        public void SetSortOrder(int sortOrder)
        {
            if (sortOrder < 0) throw new ArgumentOutOfRangeException(nameof(sortOrder));
            SortOrder = sortOrder;
        }

        /// <summary>
        /// 私有构造函数（供 EF Core 使用）
        /// </summary>
        private AdminChatSession() { }

        /// <summary>
        /// 创建新的聊天会话
        /// </summary>
        /// <param name="title">会话标题</param>
        /// <param name="userId">用户ID</param>
        public AdminChatSession(
            string title,
            int userId,
            string workspaceId = null,
            bool workspaceWriteEnabled = false,
            bool workspaceCliEnabled = false)
        {
            Title = title?.Length > 150 ? title.Substring(0, 150) : title ?? "新对话";
            UserId = userId;
            Status = ChatSessionStatus.Active;
            LastMessageTime = DateTime.Now;
            WorkspaceId = string.IsNullOrWhiteSpace(workspaceId) ? null : workspaceId.Trim();
            WorkspaceWriteEnabled = WorkspaceId != null && workspaceWriteEnabled;
            WorkspaceCliEnabled = WorkspaceId != null && workspaceCliEnabled;
        }

        /// <summary>
        /// 更新会话标题
        /// </summary>
        public void UpdateTitle(string title)
        {
            if (!string.IsNullOrEmpty(title))
            {
                Title = title.Length > 150 ? title.Substring(0, 150) : title;
                base.SetUpdateTime();
            }
        }

        /// <summary>
        /// 更新最后消息时间
        /// </summary>
        public void UpdateLastMessageTime()
        {
            LastMessageTime = DateTime.Now;
            base.SetUpdateTime();
        }

        /// <summary>
        /// 归档会话
        /// </summary>
        public void Archive()
        {
            Status = ChatSessionStatus.Archived;
            base.SetUpdateTime();
        }

        /// <summary>
        /// 删除会话（软删除，修改状态）
        /// </summary>
        public void Delete()
        {
            Status = ChatSessionStatus.Deleted;
            base.SetUpdateTime();
        }

        /// <summary>
        /// 恢复会话
        /// </summary>
        public void Restore()
        {
            Status = ChatSessionStatus.Active;
            base.SetUpdateTime();
        }
    }

    /// <summary>
    /// 聊天会话状态
    /// </summary>
    public enum ChatSessionStatus
    {
        /// <summary>
        /// 活跃中
        /// </summary>
        Active = 0,
        /// <summary>
        /// 已归档
        /// </summary>
        Archived = 1,
        /// <summary>
        /// 已删除
        /// </summary>
        Deleted = 2
    }
}
