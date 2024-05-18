using AutoGen.SemanticKernel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Senaprc.AI.Agents.AgentUtility;
using Senparc.AI.Entities;
using Senparc.AI;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Helpers;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Senparc.AI.Kernel.Handlers;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senaprc.AI.Agents.AgentExtensions;
using Senaprc.AI.Agents;
using AutoGen;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using AutoGen.Core;
using Senparc.Ncf.Core.AppServices;
using Senparc.CO2NET.Trace;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class ChatGroupService : ServiceBase<ChatGroup>
    {
        public ChatGroupService(IRepositoryBase<ChatGroup> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        public async Task<IEnumerable<IMessage>> RunGroup(AppServiceLogger logger, int id, string userCommand, ISenparcAiSetting senparcAiSetting, bool individuation)
        {
            var chatGroupMemberService = base._serviceProvider.GetService<ServiceBase<ChatGroupMember>>();
            var agentTemplateService = base._serviceProvider.GetService<AgentsTemplateService>();
            var promptItemService = base._serviceProvider.GetService<PromptItemService>();

            var chatGroup = await base.GetObjectAsync(x => x.Id == id);
            logger.Append($"开始运行 {chatGroup.Name}");

            var groupMemebers = await chatGroupMemberService.GetFullListAsync(z => z.ChatGroupId == id);
            var agentsTemplates = new List<AgentTemplate>();
            var agents = new List<SemanticKernelAgent>();
            List<MiddlewareAgent<SemanticKernelAgent>> agentsMiddlewares = new List<MiddlewareAgent<SemanticKernelAgent>>();

            var _semanticAiHandler = new SemanticAiHandler(senparcAiSetting);

            var parameter = new PromptConfigParameter()
            {
                MaxTokens = 2000,
                Temperature = 0.7,
                TopP = 0.5,
            };

            var iWantToRun = _semanticAiHandler.IWantTo(senparcAiSetting)
                            .ConfigModel(ConfigModel.Chat, "JeffreySu")
                            .BuildKernel();

            var kernel = iWantToRun.Kernel;//同一外围 Agent

            //作为唯一入口和汇报的关键人（TODO：需要增加一个设置）
            AgentTemplate enterAgentTemplate = null;
            MiddlewareAgent<SemanticKernelAgent> enterAgent = null;

            foreach (var groupMember in groupMemebers)
            {
                var agentTemplate = await agentTemplateService.GetObjectAsync(x => x.Id == groupMember.AgentTemplateId);
                agentsTemplates.Add(agentTemplate);

                //TODO：确认 Prompt 此时是否存在，如果不存在需要给出提示

                var promptResult = await promptItemService.GetWithVersionAsync(agentTemplate.PromptCode, isAvg: true);

                var itemKernel = kernel;
                if (individuation)
                {
                    var semanticAiHandler = new SemanticAiHandler(promptResult.SenparcAiSetting);
                    var iWantToRunItem = semanticAiHandler.IWantTo(senparcAiSetting)
                                .ConfigModel(ConfigModel.Chat, agentTemplate.Name + groupMember.UID)
                                .BuildKernel();
                    itemKernel = iWantToRunItem.Kernel;
                }

                var agent = new SemanticKernelAgent(
                            kernel: itemKernel,
                            name: agentTemplate.Name,
                            systemMessage: promptResult.PromptItem.Content);
                var agentMiddleware = agent
                            .RegisterTextMessageConnector()
                            .RegisterCustomPrintMessage(new PrintWechatMessageMiddleware((a, m, mStr) =>
                            {
                                AgentKeys.SendWechatMessage.Invoke(a, m, mStr);
                                logger.Append($"[{chatGroup.Name}]组 {a.Name} 发送消息：{mStr}");
                            }));

                if (enterAgentTemplate == null && agentTemplate.Name.Contains("行政"))
                {
                    //TODO：添加指定入口对接人员，参考群主
                    enterAgentTemplate = agentTemplate;
                    enterAgent = agentMiddleware;
                }
                agentsMiddlewares.Add(agentMiddleware);
                agents.Add(agent);
            }

            // Create the hearing member
            //var hearingMember = new UserProxyAgent(name: chatGroup.Name + "群友");
            var hearingMember = new DefaultReplyAgent(name: chatGroup.Name + "群友", GroupChatExtension.TERMINATE);

            // Create the group admin
            var adminAgenttemplate = await agentTemplateService.GetObjectAsync(x => x.Id == chatGroup.AdminAgentTemplateId);
            var adminPromptResult = await promptItemService.GetWithVersionAsync(adminAgenttemplate.PromptCode, isAvg: true);
            var adminKernel = kernel;
            if (individuation)
            {
                var semanticAiHandler = new SemanticAiHandler(adminPromptResult.SenparcAiSetting);
                var iWantToRunItem = semanticAiHandler.IWantTo(senparcAiSetting)
                            .ConfigModel(ConfigModel.Chat, adminAgenttemplate.Name)
                            .BuildKernel();
                adminKernel = iWantToRunItem.Kernel;
            }

            var admin = new SemanticKernelAgent(
                kernel: kernel,
                name: adminAgenttemplate.Name,
                systemMessage: adminPromptResult.PromptItem.Content)
                .RegisterTextMessageConnector();


            var graphConnector = GraphBuilder.Start()
                        .ConnectFrom(hearingMember).TwoWay(enterAgent);

            //遍历所有 agents, 两两之间运行 graphConnector.ConnectFrom(agent1).TwoWay(agent2);
            for (int i = 0; i < agentsMiddlewares.Count; i++)
            {
                for (int j = i + 1; j < agentsMiddlewares.Count; j++)
                {
                    graphConnector.ConnectFrom(agentsMiddlewares[i]).TwoWay(agentsMiddlewares[j]);
                }
            }

            var finishedGraph = graphConnector.Finish();

            var aiTeam = finishedGraph.CreateAiTeam(admin);

            try
            {
                var greetingMessage = await enterAgent.SendAsync($"你好，如果已经就绪，请告诉我们“已就位”，并和 {hearingMember.Name} 打个招呼");

                var commandMessage = new TextMessage(Role.Assistant, userCommand, hearingMember.Name);

                IEnumerable<IMessage> result = await enterAgent.SendMessageToGroupAsync(
                      groupChat: aiTeam,
                      chatHistory: [greetingMessage, commandMessage],
                      maxRound: 10);

                Console.WriteLine("Chat finished.");
                logger.Append("未完成运行："+ chatGroup.Name);

                return result;
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
                throw;
            }

        }
    }
}
