using Senparc.Ncf.Core.Models;
using System;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// AdminChatSessionModuleDto：管理后台聊天会话-模块关联数据传输对象
    /// </summary>
    public class AdminChatSessionModuleDto : DtoBase<int>
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// XNCF 模块唯一标识符
        /// </summary>
        public string XncfModuleUid { get; set; }

        /// <summary>
        /// 模块名称
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        /// 模块版本
        /// </summary>
        public string ModuleVersion { get; set; }

        /// <summary>
        /// 用于前端显示的模块名称（优先菜单名）
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 菜单名称
        /// </summary>
        public string MenuName { get; set; }

        /// <summary>
        /// 模块简要说明
        /// </summary>
        public string ModuleDescription { get; set; }

        /// <summary>
        /// 添加到会话的时间
        /// </summary>
        public DateTime AddedTime { get; set; }

        /// <summary>
        /// 从实体映射到 DTO
        /// </summary>
        public static AdminChatSessionModuleDto CreateFromEntity(AdminChatSessionModule entity)
        {
            if (entity == null) return null;

            return new AdminChatSessionModuleDto
            {
                // 明确复制基类属性
                Id = entity.Id,
                AddTime = entity.AddTime,
                LastUpdateTime = entity.LastUpdateTime,
                TenantId = entity.TenantId,
                Flag = entity.Flag,

                // 复制业务属性
                SessionId = entity.SessionId,
                XncfModuleUid = entity.XncfModuleUid,
                ModuleName = entity.ModuleName,
                ModuleVersion = entity.ModuleVersion,
                DisplayName = entity.ModuleName,
                AddedTime = entity.AddedTime
            };
        }
    }
}
