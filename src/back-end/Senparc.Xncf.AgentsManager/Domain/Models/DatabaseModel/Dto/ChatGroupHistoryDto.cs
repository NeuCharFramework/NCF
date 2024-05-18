using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// ChatGroupHistory 数据库实体 DTO
    /// </summary>
    public class ChatGroupHistoryDto : DtoBase
    {
        public int ChatGroupId { get; private set; }

        public ChatGroup ChatGroup { get; private set; }

        public int FromAgentTemplateId { get; private set; }

        public AgentTemplate FromAgentTemplate { get; private set; }

        public int ToAgentTemplateId { get; private set; }

        public AgentTemplate ToAgentTemplate { get; private set; }

        public int FromChatGroupMemberId { get; private set; }

        public ChatGroupMember FromChatGroupMember { get; private set; }

        public int ToChatGroupMemberId { get; private set; }

        public ChatGroupMember ToChatGroupMember { get; private set; }

        public string Message { get; private set; }

        public MessageType MessageType { get; private set; }

        private ChatGroupHistoryDto() { }

        public ChatGroupHistoryDto(int chatGroupId, ChatGroup chatGroup, int fromAgentTemplateId, AgentTemplate fromAgentTemplate, int toAgentTemplateId, AgentTemplate toAgentTemplate, int fromChatGroupMemberId, ChatGroupMember fromChatGroupMember, int toChatGroupMemberId, ChatGroupMember toChatGroupMember, string message, MessageType messageType)
        {
            ChatGroupId = chatGroupId;
            ChatGroup = chatGroup;
            FromAgentTemplateId = fromAgentTemplateId;
            FromAgentTemplate = fromAgentTemplate;
            ToAgentTemplateId = toAgentTemplateId;
            ToAgentTemplate = toAgentTemplate;
            FromChatGroupMemberId = fromChatGroupMemberId;
            FromChatGroupMember = fromChatGroupMember;
            ToChatGroupMemberId = toChatGroupMemberId;
            ToChatGroupMember = toChatGroupMember;
            Message = message;
            MessageType = messageType;
        }

        public ChatGroupHistoryDto(ChatGroupHistory chatGroupHistory)
        {
            ChatGroupId = chatGroupHistory.ChatGroupId;
            ChatGroup = chatGroupHistory.ChatGroup;
            FromAgentTemplateId = chatGroupHistory.FromAgentTemplateId;
            FromAgentTemplate = chatGroupHistory.FromAgentTemplate;
            ToAgentTemplateId = chatGroupHistory.ToAgentTemplateId;
            ToAgentTemplate = chatGroupHistory.ToAgentTemplate;
            FromChatGroupMemberId = chatGroupHistory.FromChatGroupMemberId;
            FromChatGroupMember = chatGroupHistory.FromChatGroupMember;
            ToChatGroupMemberId = chatGroupHistory.ToChatGroupMemberId;
            ToChatGroupMember = chatGroupHistory.ToChatGroupMember;
            Message = chatGroupHistory.Message;
            MessageType = chatGroupHistory.MessageType;
        }
    }
}
