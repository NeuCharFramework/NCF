using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    /// AdminChatSessionModule：管理后台聊天会话-模块关联
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatSessionModule))]
    [Serializable]
    public class AdminChatSessionModule : EntityBase<int>
    {
        /// <summary>
        /// 会话ID（外键到 AdminChatSession）
        /// </summary>
        [Required]
        public int SessionId { get; private set; }

        /// <summary>
        /// XNCF 模块唯一标识符
        /// </summary>
        [Required, MaxLength(200)]
        public string XncfModuleUid { get; private set; }

        /// <summary>
        /// 模块名称（冗余存储，便于快速查询）
        /// </summary>
        [Required, MaxLength(200)]
        public string ModuleName { get; private set; }

        /// <summary>
        /// 模块版本（冗余存储）
        /// </summary>
        [MaxLength(50)]
        public string ModuleVersion { get; private set; }

        /// <summary>
        /// 添加到会话的时间
        /// </summary>
        [Required]
        public DateTime AddedTime { get; private set; }

        /// <summary>
        /// 导航属性：关联的会话
        /// </summary>
        [ForeignKey(nameof(SessionId))]
        public virtual AdminChatSession Session { get; private set; }

        /// <summary>
        /// 私有构造函数（供 EF Core 使用）
        /// </summary>
        private AdminChatSessionModule() { }

        /// <summary>
        /// 创建新的会话-模块关联
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="xncfModuleUid">XNCF 模块 UID</param>
        /// <param name="moduleName">模块名称</param>
        /// <param name="moduleVersion">模块版本</param>
        public AdminChatSessionModule(int sessionId, string xncfModuleUid, string moduleName, string moduleVersion)
        {
            SessionId = sessionId;
            XncfModuleUid = xncfModuleUid ?? throw new ArgumentNullException(nameof(xncfModuleUid));
            ModuleName = moduleName ?? "未知模块";
            ModuleVersion = moduleVersion;
            AddedTime = DateTime.Now;
        }
    }
}
