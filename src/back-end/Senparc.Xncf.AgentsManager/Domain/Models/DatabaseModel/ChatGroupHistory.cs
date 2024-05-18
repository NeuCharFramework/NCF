using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    /// ChatGroupHistory 数据库实体
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(ChatGroupHistory))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class ChatGroupHistory : EntityBase<int>
    {
        [Required]
        public int ChatGroupId { get; private set; }

        [Required]
        public ChatGroup ChatGroup { get; private set; }

        [ForeignKey(nameof(FromAgentTemplate))]
        public int? FromAgentTemplateId { get; private set; }
        [InverseProperty(nameof(AgentTemplate.FromChatGroupHistories))]
        public AgentTemplate FromAgentTemplate { get; private set; }

        [ForeignKey(nameof(ToAgentTemplate))]
        public int? ToAgentTemplateId { get; private set; }

        [InverseProperty(nameof(AgentTemplate.ToChatGroupHistoies))]
        public AgentTemplate ToAgentTemplate { get; private set; }

        //[ForeignKey(nameof(FromChatGroupMember))]
        //public int? FromChatGroupMemberId { get; private set; }

        //public ChatGroupMember FromChatGroupMember { get; private set; }

        //[ForeignKey(nameof(ToChatGroupMember))]
        //public int? ToChatGroupMemberId { get; private set; }

        //public ChatGroupMember ToChatGroupMember { get; private set; }

        [Required]
        public string Message { get; private set; }

        [Required]
        public MessageType MessageType { get; private set; }

        private ChatGroupHistory() { }

        public ChatGroupHistory(int chatGroupId, ChatGroup chatGroup, int? fromAgentTemplateId, AgentTemplate fromAgentTemplate, int? toAgentTemplateId, AgentTemplate toAgentTemplate, /*int? fromChatGroupMemberId, ChatGroupMember fromChatGroupMember, int? toChatGroupMemberId, ChatGroupMember toChatGroupMember,*/ string message, MessageType messageType)
        {
            ChatGroupId = chatGroupId;
            ChatGroup = chatGroup;
            FromAgentTemplateId = fromAgentTemplateId;
            FromAgentTemplate = fromAgentTemplate;
            ToAgentTemplateId = toAgentTemplateId;
            ToAgentTemplate = toAgentTemplate;
            //FromChatGroupMemberId = fromChatGroupMemberId;
            //FromChatGroupMember = fromChatGroupMember;
            //ToChatGroupMemberId = toChatGroupMemberId;
            //ToChatGroupMember = toChatGroupMember;
            Message = message;
            MessageType = messageType;
        }

        public ChatGroupHistory(ChatGroupHistoryDto chatGroupHistoryDto)
        {
            ChatGroupId = chatGroupHistoryDto.ChatGroupId;
            ChatGroup = chatGroupHistoryDto.ChatGroup;
            FromAgentTemplateId = chatGroupHistoryDto.FromAgentTemplateId;
            FromAgentTemplate = chatGroupHistoryDto.FromAgentTemplate;
            ToAgentTemplateId = chatGroupHistoryDto.ToAgentTemplateId;
            ToAgentTemplate = chatGroupHistoryDto.ToAgentTemplate;
            //FromChatGroupMemberId = chatGroupHistoryDto.FromChatGroupMemberId;
            //FromChatGroupMember = chatGroupHistoryDto.FromChatGroupMember;
            //ToChatGroupMemberId = chatGroupHistoryDto.ToChatGroupMemberId;
            //ToChatGroupMember = chatGroupHistoryDto.ToChatGroupMember;
            Message = chatGroupHistoryDto.Message;
            MessageType = chatGroupHistoryDto.MessageType;
        }
    }

    public enum MessageType
    {
        Text,
        Image,
        Voice,
        Video
    }
}
