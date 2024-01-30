using Senparc.Ncf.Core.Models;
using System;

namespace SenparcConf.Xncf.WechatBot.Models.DatabaseModel.Dto
{
    public class MessageDto : DtoBase
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

        private MessageDto() { }
    }
}