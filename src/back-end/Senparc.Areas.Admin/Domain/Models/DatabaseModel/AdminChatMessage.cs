using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    /// AdminChatMessage：管理后台聊天消息
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatMessage))]
    [Serializable]
    public class AdminChatMessage : EntityBase<int>
    {
        /// <summary>
        /// 所属会话ID（外键到 AdminChatSession）
        /// </summary>
        [Required]
        public int SessionId { get; private set; }

        /// <summary>
        /// 消息角色类型（User/Assistant/System）
        /// </summary>
        [Required]
        public ChatMessageRoleType RoleType { get; private set; }

        /// <summary>
        /// 消息内容（支持 Markdown 格式）
        /// </summary>
        [Required]
        public string Content { get; private set; }

        /// <summary>
        /// 消息序号（用于保持对话顺序）
        /// </summary>
        [Required]
        public int Sequence { get; private set; }

        /// <summary>
        /// 用户反馈（Like/Dislike/None）
        /// </summary>
        public MessageFeedbackType UserFeedback { get; private set; }

        /// <summary>
        /// 使用的模型标识符（例如："gpt-4", "claude-3"）
        /// </summary>
        [MaxLength(100)]
        public string ModelIdentifier { get; private set; }

        /// <summary>
        /// 导航属性：关联的会话
        /// </summary>
        [ForeignKey(nameof(SessionId))]
        public virtual AdminChatSession Session { get; private set; }

        /// <summary>
        /// 私有构造函数（供 EF Core 使用）
        /// </summary>
        private AdminChatMessage() { }

        /// <summary>
        /// 创建新的聊天消息
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="roleType">角色类型</param>
        /// <param name="content">消息内容</param>
        /// <param name="sequence">消息序号</param>
        /// <param name="modelIdentifier">模型标识符（可选）</param>
        public AdminChatMessage(int sessionId, ChatMessageRoleType roleType, string content, int sequence, string modelIdentifier = null)
        {
            SessionId = sessionId;
            RoleType = roleType;
            Content = content ?? string.Empty;
            Sequence = sequence;
            UserFeedback = MessageFeedbackType.None;
            ModelIdentifier = modelIdentifier;
        }

        /// <summary>
        /// 设置用户反馈
        /// </summary>
        public void SetFeedback(MessageFeedbackType feedback)
        {
            UserFeedback = feedback;
            base.SetUpdateTime();
        }

        /// <summary>
        /// 更新消息内容（通常用于流式输出场景的追加）
        /// </summary>
        public void UpdateContent(string newContent)
        {
            if (newContent != null)
            {
                Content = newContent;
                base.SetUpdateTime();
            }
        }
    }

    /// <summary>
    /// 聊天消息角色类型
    /// </summary>
    public enum ChatMessageRoleType
    {
        /// <summary>
        /// 用户消息
        /// </summary>
        User = 0,
        /// <summary>
        /// AI 助手消息
        /// </summary>
        Assistant = 1,
        /// <summary>
        /// 系统消息
        /// </summary>
        System = 2
    }

    /// <summary>
    /// 消息反馈类型
    /// </summary>
    public enum MessageFeedbackType
    {
        /// <summary>
        /// 无反馈
        /// </summary>
        None = 0,
        /// <summary>
        /// 点赞
        /// </summary>
        Like = 1,
        /// <summary>
        /// 点踩
        /// </summary>
        Dislike = 2
    }
}
