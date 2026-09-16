/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatMessageDto.cs
    文件功能描述：AdminChatMessageDto 相关功能实现


    创建标识：Senparc - 20260325

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/
using Senparc.Areas.Admin.Domain.Services;
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

        public int? TrajectoryId { get; set; }

        public int? TrajectorySequence { get; set; }

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
                ModelIdentifier = entity.ModelIdentifier,
                TrajectoryId = entity.TrajectoryId,
                TrajectorySequence = entity.TrajectorySequence
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

        /// <summary>
        /// 运行模式：Simple（普通单轮对话，默认）或 Harness（MAF 长任务）。
        /// </summary>
        public AdminChatMode Mode { get; set; } = AdminChatMode.Simple;
    }
}
