using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    /// ChatGroup 数据库实体
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(ChatGroup))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class ChatGroup : EntityBase<int>
    {
        /// <summary>
        /// 群名称
        /// </summary>
        [Required]
        public string Name { get; private set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Required]
        public bool Enable { get; private set; }

        /// <summary>
        /// 状态
        /// </summary>
        [Required]
        public ChatGroupState State { get; private set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// 管理员代理模板Id
        /// </summary>
        [Required]
        [ForeignKey(nameof(AdminAgentTemplate))]
        public int AdminAgentTemplateId { get; private set; }

        public AgentTemplate AdminAgentTemplate { get; set; }

        //public ICollection<ChatGroupMember> ChatGroupMembers { get; set; }


        private ChatGroup() { }

        public ChatGroup(string name, bool enable, ChatGroupState state, string description, int adminAgentTemplateId)
        {
            Name = name;
            Enable = enable;
            State = state;
            Description = description;
            AdminAgentTemplateId = adminAgentTemplateId;
        }

        public ChatGroup(ChatGroupDto chatGroupDto)
        {
            Name = chatGroupDto.Name;
            Enable = chatGroupDto.Enable;
            State = chatGroupDto.State;
            Description = chatGroupDto.Description;
            AdminAgentTemplateId = chatGroupDto.AdminAgentTemplateId;
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

    /// <summary>
    /// 群状态
    /// </summary>
    public enum ChatGroupState
    {
        /// <summary>
        /// 未开始
        /// </summary>
        Unstart = 0,
        /// <summary>
        /// 运行中
        /// </summary>
        Running = 1,
        /// <summary>
        /// 已结束
        /// </summary>
        Finished = 2,
    }
}
