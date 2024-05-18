using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// ChatGroupMemer 数据库实体 DTO
    /// </summary>
    public class ChatGroupMemberDto : DtoBase
    {
        /// <summary>
        /// UID
        /// </summary>
        public string UID { get; private set; }

        /// <summary>
        /// AgentTemplateId
        /// </summary>
        public int AgentTemplateId { get; private set; }

        /// <summary>
        /// AgentTemplate（类型同名）
        /// </summary>
        public AgentTemplate AgentTemplate { get; private set; }

        ///// <summary>
        ///// ChatGroupId
        ///// </summary>
        //public int ChatGroupId { get; private set; }

        ///// <summary>
        ///// ChatGroup（类型同名）
        ///// </summary>
        //public ChatGroup ChatGroup { get; private set; }

        private ChatGroupMemberDto() { }

        public ChatGroupMemberDto(string uID, int agentTemplateId, AgentTemplate agentTemplate/*, int chatGroupId, ChatGroup chatGroup*/)
        {
            UID = uID;
            AgentTemplateId = agentTemplateId;
            AgentTemplate = agentTemplate;
            //ChatGroupId = chatGroupId;
            //ChatGroup = chatGroup;
        }

        public ChatGroupMemberDto(ChatGroupMember chatGroupMemer)
        {
            UID = chatGroupMemer.UID;
            AgentTemplateId = chatGroupMemer.AgentTemplateId;
            AgentTemplate = chatGroupMemer.AgentTemplate;
            //ChatGroupId = chatGroupMemer.ChatGroupId;
            //ChatGroup = chatGroupMemer.ChatGroup;
        }
    }
}
