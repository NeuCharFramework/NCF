using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// ChatGroup 数据库实体 DTO
    /// </summary>
    public class ChatGroupDto : DtoBase
    {
        /// <summary>
        /// 群名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; private set; }

        /// <summary>
        /// 状态
        /// </summary>
        public ChatGroupState State { get; private set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// 管理员代理模板Id
        /// </summary>
        public int AdminAgentTemplateId { get; private set; }

        public AgentTemplate AdminAgentTemplate { get; set; }

        /// <summary>
        /// 对接人代理模板Id
        /// </summary>

        public int EnterAgentTemplateId { get; private set; }

        public AgentTemplate EnterAgentTemplate { get; set; }

        private ChatGroupDto() { }

        public ChatGroupDto(string name, bool enable, ChatGroupState state, string description, int adminAgentTemplateId, int enterAgentTemplateId)
        {
            Name = name;
            Enable = enable;
            State = state;
            Description = description;
            AdminAgentTemplateId = adminAgentTemplateId;
            EnterAgentTemplateId = enterAgentTemplateId;
        }

        public ChatGroupDto(ChatGroup chatGroup)
        {
            Name = chatGroup.Name;
            Enable = chatGroup.Enable;
            State = chatGroup.State;
            Description = chatGroup.Description;
            AdminAgentTemplateId = chatGroup.AdminAgentTemplateId;
            EnterAgentTemplateId = chatGroup.EnterAgentTemplateId;
        }

        public void Start()
        {
            State = ChatGroupState.Running;
        }

        public void Finish()
        {
            State = ChatGroupState.Finished;
        }
    }
}
