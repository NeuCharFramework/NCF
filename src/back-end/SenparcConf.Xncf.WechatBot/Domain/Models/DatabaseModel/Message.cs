using Senparc.Ncf.Core.Models;
using SenparcConf.Xncf.WechatBot.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenparcConf.Xncf.WechatBot.Models
{
    /// <summary>
    /// Message 数据库实体
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Message))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class Message : EntityBase<int>
    {
        /// <summary>
        /// 收到的消息
        /// </summary>
        public string ReceivedMessage { get; private set; }

        /// <summary>
        /// 返回的消息
        /// </summary>
        public string ReturnMessage { get; private set; }

        /// <summary>
        /// 查询消息使用时间
        /// </summary>
        public DateTime QueryTime { get; private set; }

        /// <summary>
        /// AccountId
        /// </summary>
        public int AccountId { get; private set; }

        private Message() { }

        public Message(string receivedMessage, string returnMessage, DateTime queryTime, int accountId)
        {
            ReceivedMessage = receivedMessage;
            ReturnMessage = returnMessage;
            QueryTime = queryTime;
            AccountId = accountId;
        }

        public Message(MessageDto messageDto)
        {
            ReceivedMessage = messageDto.ReceivedMessage;
            ReturnMessage = messageDto.ReturnMessage;
            QueryTime = messageDto.QueryTime;
            AccountId = messageDto.AccountId;
        }
    }
}