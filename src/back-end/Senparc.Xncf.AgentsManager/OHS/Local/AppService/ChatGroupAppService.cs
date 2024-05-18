using AutoMapper.Execution;
using log4net.Repository;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    public class ChatGroupAppService : AppServiceBase
    {
        private readonly ChatGroupService _chatGroupService;
        private readonly ServiceBase<ChatGroupMember> _chatGroupMemeberService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly AIModelService _aIModelService;

        public ChatGroupAppService(IServiceProvider serviceProvider,
            ChatGroupService chatGroupService,
            ServiceBase<ChatGroupMember> chatGroupMemeberService,
            AgentsTemplateService agentsTemplateService,
            AIModelService aIModelService) : base(serviceProvider)
        {
            this._chatGroupService = chatGroupService;
            this._chatGroupMemeberService = chatGroupMemeberService;
            this._agentsTemplateService = agentsTemplateService;
            this._aIModelService = aIModelService;
        }

        [FunctionRender("管理 ChatGroup", "管理 ChatGroup", typeof(Register))]
        public async Task<StringAppResponse> ManageChatGroupManage(ChatGroup_ManageChatGroupRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                //群主
                if (request.Admin.SelectedValues.Count() == 0 || !int.TryParse(request.Admin.SelectedValues.First(), out int adminId))
                {
                    return "必须选择一位群主，请到 AgentTemplate 中设置！";
                }

                var agentsTemplateAdmin = await _agentsTemplateService.GetAgentTemplateAsync(adminId);

                SenparcAI_GetByVersionResponse promptResult;

                //TODO:封装到 Service 中
                ChatGroup chatGroup = null;
                var chatGroupDto = new ChatGroupDto(request.Name, true, ChatGroupState.Unstart, request.Description, agentsTemplateAdmin.Id);
                var isNew = false;
                if (request.ChatGroup.IsSelected("New"))
                {
                    //新建
                    chatGroup = new ChatGroup(chatGroupDto);
                    isNew = false;
                }
                else
                {
                    int.TryParse(request.ChatGroup.SelectedValues.First(), out int chatGroupId);
                    chatGroup = await _chatGroupService.GetObjectAsync(z => z.Id == chatGroupId);
                    _chatGroupService.Mapper.Map<ChatGroup>(chatGroupDto);
                }

                await _chatGroupService.SaveObjectAsync(chatGroup);

                logger.Append($"ChatGroup {(isNew ? "新增" : "编辑")} 成功！");


                //添加成员
                var memberList = new List<ChatGroupMember>();
                foreach (var agentId in request.Members.SelectedValues.Select(z => int.Parse(z)))
                {
                    var chatGroupMemberDto = new ChatGroupMemberDto(null, chatGroup.Id, agentId);
                    var member = new ChatGroupMember(chatGroupMemberDto);
                    member.ResetUID();
                    memberList.Add(member);
                }
                await _chatGroupMemeberService.SaveObjectListAsync(memberList);

                logger.Append($"ChatGroup 成员添加成功！");

                return logger.ToString();
            });
        }

        [FunctionRender("启动 ChatGroup", "启动 ChatGroup", typeof(Register))]
        public async Task<StringAppResponse> RunChatGroup(ChatGroup_RunChatGroupRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                //群主
                if (request.ChatGroups.SelectedValues.Count() == 0)
                {
                    return "至少选择一个组！";
                }

                var aiModelSelected = request.AIModel.SelectedValues.FirstOrDefault();
                var aiSetting = Senparc.AI.Config.SenparcAiSetting;
                if (aiModelSelected != "Default")
                {
                    int.TryParse(aiModelSelected, out int aiModelId);
                    var aiModel = await _aIModelService.GetObjectAsync(z => z.Id == aiModelId);
                    if (aiModel == null)
                    {
                        throw new NcfExceptionBase($"当前选择的 AI 模型不存在：{aiModelSelected}");
                    }

                    var aiModelDto = _aIModelService.Mapper.Map<AIModelDto>(aiModel);

                    aiSetting = _aIModelService.BuildSenparcAiSetting(aiModelDto);
                }

                List<Task> tasks = new List<Task>();

                foreach (var chatGroupId in request.ChatGroups.SelectedValues.Select(z => int.Parse(z)))
                {
                    var task = _chatGroupService.RunGroup(logger, chatGroupId, aiSetting, request.Individuation.IsSelected("1"));
                    tasks.Add(task);
                }

                Task.WaitAll(tasks.ToArray());

                return logger.ToString();
            });
        }
    }
}
