using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// AdminChatMessageDto：管理后台聊天消息数据传输对象
    /// </summary>
    public class AdminChatMessageDto : DtoBase<int>
    {
        /// <summary>
        /// 所属会话ID
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// 消息角色类型
        /// </summary>
        public ChatMessageRoleType RoleType { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 消息序号
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// 用户反馈
        /// </summary>
        public MessageFeedbackType UserFeedback { get; set; }

        /// <summary>
        /// 使用的模型标识符
        /// </summary>
        public string ModelIdentifier { get; set; }

        /// <summary>
        /// 从实体映射到 DTO
        /// </summary>
        public static AdminChatMessageDto CreateFromEntity(AdminChatMessage entity)
        {
            if (entity == null) return null;

            return new AdminChatMessageDto
            {
                // 明确复制基类属性
                Id = entity.Id,
                AddTime = entity.AddTime,
                LastUpdateTime = entity.LastUpdateTime,
                TenantId = entity.TenantId,
                Flag = entity.Flag,

                // 复制业务属性
                SessionId = entity.SessionId,
                RoleType = entity.RoleType,
                Content = entity.Content,
                Sequence = entity.Sequence,
                UserFeedback = entity.UserFeedback,
                ModelIdentifier = entity.ModelIdentifier
            };
        }
    }

    /// <summary>
    /// 发送聊天消息的请求 DTO
    /// </summary>
    public class ChatMessageInputDto
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        [Required]
        public int SessionId { get; set; }

        /// <summary>
        /// 选中的 AIModelId，0 表示系统级 SenparcAiSetting
        /// </summary>
        public int AiModelId { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        [Required]
        public string Content { get; set; }
    }
}
