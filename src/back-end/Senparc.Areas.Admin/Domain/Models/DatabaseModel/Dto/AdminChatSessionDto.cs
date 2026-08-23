/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatSessionDto.cs
    文件功能描述：AdminChatSessionDto.cs 相关实现


    创建标识：Senparc - 20260325

    修改标识：Senparc - 20260822
    修改描述：v0.6.0 新增管理端 Chat 会话工作流能力

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// AdminChatSessionDto：管理后台聊天会话数据传输对象
    /// </summary>
    public class AdminChatSessionDto : DtoBase<int>
    {
        /// <summary>
        /// 会话标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 会话状态
        /// </summary>
        public ChatSessionStatus Status { get; set; }

        /// <summary>
        /// 最后一条消息时间
        /// </summary>
        public DateTime LastMessageTime { get; set; }

        /// <summary>
        /// 关联的消息列表（可选，用于嵌套查询）
        /// </summary>
        public List<AdminChatMessageDto> Messages { get; set; }

        /// <summary>
        /// 关联的模块列表（可选，用于嵌套查询）
        /// </summary>
        public List<AdminChatSessionModuleDto> Modules { get; set; }

        /// <summary>
        /// 关联的 Workflow 列表（可选，用于嵌套查询）
        /// </summary>
        public List<AdminChatSessionWorkflowDto> Workflows { get; set; }

        public AdminChatSessionDto() 
        {
            Messages = new List<AdminChatMessageDto>();
            Modules = new List<AdminChatSessionModuleDto>();
            Workflows = new List<AdminChatSessionWorkflowDto>();
        }

        /// <summary>
        /// 从实体映射到 DTO
        /// </summary>
        public static AdminChatSessionDto CreateFromEntity(AdminChatSession entity)
        {
            if (entity == null) return null;

            return new AdminChatSessionDto
            {
                // 明确复制基类属性
                Id = entity.Id,
                AddTime = entity.AddTime,
                LastUpdateTime = entity.LastUpdateTime,
                TenantId = entity.TenantId,
                Flag = entity.Flag,

                // 复制业务属性
                Title = entity.Title,
                UserId = entity.UserId,
                Status = entity.Status,
                LastMessageTime = entity.LastMessageTime
            };
        }
    }

    /// <summary>
    /// 创建聊天会话的请求 DTO
    /// </summary>
    public class CreateChatSessionInputDto
    {
        /// <summary>
        /// 初始消息内容
        /// </summary>
        public string InitialMessage { get; set; }

        /// <summary>
        /// 选中的 AIModelId，0 表示系统级 SenparcAiSetting
        /// </summary>
        public int AiModelId { get; set; }

        /// <summary>
        /// 关联的模块 UID 列表
        /// </summary>
        public List<string> ModuleUids { get; set; }

        /// <summary>
        /// 选中的 Workflow Id 列表
        /// </summary>
        public List<int> WorkflowIds { get; set; }

        public CreateChatSessionInputDto()
        {
            ModuleUids = new List<string>();
            WorkflowIds = new List<int>();
        }
    }
}
