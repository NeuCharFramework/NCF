using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class AgentTemplate_ManageRequest : FunctionAppRequestBase
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

        [Required]
        [Description("外接平台||需要对外发布消息的平台")]
        public SelectionList HookRobotType { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());
        //TODO:可以选择多个通道


        [Description("外界平台参数||通常为 Key 之类的参数")]
        public string HookRobotParameter { get; set; }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            await base.LoadData(serviceProvider);


            var hookRobotTypeItems = Enum.GetValues<HookRobotType>();
            foreach (var item in hookRobotTypeItems)
            {
                HookRobotType.Items.Add(new SelectionItem(((int)item).ToString(), item.ToString(), item.ToString(), false));
            }

        }
    }

}
