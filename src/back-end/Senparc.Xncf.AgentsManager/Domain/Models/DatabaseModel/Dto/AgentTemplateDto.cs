using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// Agent模板信息
    /// </summary>
    public class AgentTemplateDto : DtoBase
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 系统消息
        /// </summary>
        public string SystemMessage { get; private set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; private set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// PromptRange 的代号
        /// </summary>
        public string PromptCode { get; set; }

        private AgentTemplateDto() { }

        public AgentTemplateDto(string name, string systemMessage, bool enable, string description, string promptCode = null)
        {
            Name = name;
            SystemMessage = systemMessage;
            Enable = enable;
            Description = description;
            PromptCode = promptCode;
        }
    }
}
