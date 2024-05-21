using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel
{
    /// <summary>
    /// Agent模板信息
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AgentTemplate))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class AgentTemplate : EntityBase<int>
    {
        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        public string Name { get; private set; }

        /// <summary>
        /// 系统消息
        /// </summary>
        [Required]
        public string SystemMessage { get; private set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Required]
        public bool Enable { get; private set; }

        /// <summary>
        /// PromptRange 的代号
        /// </summary>
        public string PromptCode { get; private set; }

        /// <summary>
        /// 第三方机器人平台类型
        /// </summary>
        public HookRobotType HookRobotType { get; private set; }

        /// <summary>
        /// 第三方机器人平台参数
        /// </summary>
        public string HookRobotParameter { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; private set; }


        //[InverseProperty(nameof(ChatGroupMember.AgentTemplate))]
        public List<ChatGroupMember> ChatGroupMembers { get; private set; }

        //[InverseProperty(nameof(ChatGroup.AdminAgentTemplate))]
        public ICollection<ChatGroup> AdminChatGroups { get; private set; }

        //[InverseProperty(nameof(ChatGroup.EnterAgentTemplate))]
        public ICollection<ChatGroup> EnterAgentChatGroups { get; private set; }

        [InverseProperty(nameof(ChatGroupHistory.FromAgentTemplate))]
        public ICollection<ChatGroupHistory> FromChatGroupHistories { get; set; }

        [InverseProperty(nameof(ChatGroupHistory.ToAgentTemplate))]
        public ICollection<ChatGroupHistory> ToChatGroupHistoies { get; set; }

        private AgentTemplate() { }

        public AgentTemplate(string name, string systemMessage, bool enable, string description, string promptCode, HookRobotType hookRobotType, string hookRobotParameter)
        {
            Name = name;
            SystemMessage = systemMessage;
            Enable = enable;
            Description = description;
            PromptCode = promptCode;
            HookRobotType = hookRobotType;
            HookRobotParameter = hookRobotParameter;
        }

        public bool EnableAgent()
        {
            Enable = true;
            return true;
        }

        public bool DisableAgent()
        {
            Enable = false;
            return true;
        }
    }

    /// <summary>
    /// 第三方机器人平台类型
    /// </summary>
    public enum HookRobotType
    {
        None = 0,
        /// <summary>
        /// 微信公众号
        /// </summary>
        WeChatMp = 1,
        /// <summary>
        /// 企业微信机器人
        /// </summary>
        WeChatWorkRobot = 2
    }
}
