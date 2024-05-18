using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    /// ChatGroupMemer 数据库实体
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(ChatGroupMember))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class ChatGroupMember : EntityBase<int>
    {
        /// <summary>
        /// UID
        /// </summary>
        [Required]
        public string UID { get; private set; }

        /// <summary>
        /// AgentTemplateId
        /// </summary>
        [Required]
        [ForeignKey(nameof(AgentTemplate))]
        public int AgentTemplateId { get; private set; }

        /// <summary>
        /// AgentTemplate（类型同名）
        /// </summary>
        public AgentTemplate AgentTemplate { get; private set; }

        /// <summary>
        /// ChatGroupId
        /// </summary>
        [Required]
        public int ChatGroupId { get; private set; }

        ///// <summary>
        ///// ChatGroup（类型同名）
        ///// </summary>
        //public ChatGroup ChatGroup { get; private set; }

        //[InverseProperty(nameof(ChatGroupHistory.FromChatGroupMember))]
        //public List<ChatGroupHistory> FromChatGroupHistories { get; set; }

        //[InverseProperty(nameof(ChatGroupHistory.ToChatGroupMember))]
        //public List<ChatGroupHistory> ToChatGroupHistories { get; set; }


        private ChatGroupMember() { }

        public ChatGroupMember(int agentTemplateId, AgentTemplate agentTemplate/*, int chatGroupId, ChatGroup chatGroup*/)
        {
            ResetUID();
            AgentTemplateId = agentTemplateId;
            AgentTemplate = agentTemplate;
            //ChatGroupId = chatGroupId;
            //ChatGroup = chatGroup;
        }

        public ChatGroupMember(ChatGroupMemberDto chatGroupMemerDto)
        {
            UID = chatGroupMemerDto.UID;
            AgentTemplateId = chatGroupMemerDto.AgentTemplateId;
            AgentTemplate = chatGroupMemerDto.AgentTemplate;
            //ChatGroupId = chatGroupMemerDto.ChatGroupId;
            //ChatGroup = chatGroupMemerDto.ChatGroup;
        }

        /// <summary>
        /// 重新设置 UID
        /// </summary>
        public void ResetUID()
        {
            UID = Guid.NewGuid().ToString("N");
        }
    }
}
