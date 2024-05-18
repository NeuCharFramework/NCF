using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.XncfBuilder.OHS.PL;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class ChatGroup_ManageChatGroupRequest : FunctionAppRequestBase
    {
        [Description("选择组||选择需要操作的组，或新增")]
        public SelectionList ChatGroup { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("New","新建组","新建",true)
            });

        [Required]
        [MaxLength(30)]
        [Description("群名称||群名称")]
        public string Name { get; set; }

        [Required]
        [Description("群成员||群成员")]
        public SelectionList Members { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>());

        [Required]
        [Description("群主||群主")]
        public SelectionList Admin { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [MaxLength(200)]
        [Description("说明||说明")]
        public string Description { get; set; }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            //ChatGroup
            var chatGroupService = serviceProvider.GetService<ServiceBase<ChatGroup>>();
            var chatGroups = await chatGroupService.GetFullListAsync(z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);

            chatGroups.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description))
                .ToList().ForEach(x => ChatGroup.Items.Add(x));

            //Agent
            var agentTemplateService = serviceProvider.GetService<AgentsTemplateService>();
            var agents = await agentTemplateService.GetFullListAsync(z => z.Enable, z => z.Name, Ncf.Core.Enums.OrderingType.Ascending);

            Members.Items = agents.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description)).ToList();
            Admin.Items = agents.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description)).ToList();

            var admin = Admin.Items.FirstOrDefault(z => z.Text == "群主");
            if (admin != null)
            {
                admin.DefaultSelected = true;
            }

            await base.LoadData(serviceProvider);
        }
    }

    public class ChatGroup_RunChatGroupRequest : FunctionAppRequestBase
    {
        [Description("选择组||选择需要运行的组")]
        public SelectionList ChatGroups { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>());

        [Description("AI 模型||请选择运行此程序的外围 AI 模型")]
        public SelectionList AIModel { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>
        {
            //new SelectionItem("Default","系统默认","通过系统默认配置的固定 AI 模型信息",true)
        });

        [Description("个性化智能体||")]
        public SelectionList Individuation { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>
        {
            new SelectionItem("1","是","采用个性化 AI 参数运行 Agent",true)
        });


        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            //ChatGroup
            var chatGroupService = serviceProvider.GetService<ServiceBase<ChatGroup>>();
            var chatGroups = await chatGroupService.GetFullListAsync(z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);

            chatGroups.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description))
                .ToList().ForEach(x => ChatGroups.Items.Add(x));

            //载入 AI 模型
            await BuildXncfRequestHelper.LoadAiModelData(serviceProvider, AIModel);

            await base.LoadData(serviceProvider);
        }
    }
}
