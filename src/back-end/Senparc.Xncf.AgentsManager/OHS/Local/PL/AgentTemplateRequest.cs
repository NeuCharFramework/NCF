using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class AgentTemplate_ManageRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(30)]
        [Description("名称||Agent 模板名称")]
        public string Name { get; set; }

        [Required]
        [Description("Id||如果为 0 则新增")]
        public int Id { get; set; }

        [Required]
        [Description("SystemMessage||SystemMessage 的 PromptRangeCode（支持自选模式）")]
        public string SystemMessagePromptCode { get; set; }

        [Description("说明||对 Agent Template 进行说明，此信息不会对模型效果产生影响")]
        public string Description { get; set; }

    }
}
