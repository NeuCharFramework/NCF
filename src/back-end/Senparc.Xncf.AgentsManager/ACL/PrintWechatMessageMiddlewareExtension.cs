using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoGen.Core;
using Senparc.CO2NET.Trace;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;

namespace Senparc.Xncf.AgentsManager.ACL
{
    public static class PrintWechatMessageMiddlewareExtension
    {
        public static Action<IAgent, IMessage, string, AgentTemplateDto> SendWechatMessage => async (agent, replyObject, message, agentTemplateDto) =>
         {
             string key = null;
             switch (agentTemplateDto.HookRobotType)
             {
                 case HookRobotType.None:
                     break;
                 case HookRobotType.WeChatMp:
                     break;
                 case HookRobotType.WeChatWorkRobot:
                     key = agentTemplateDto.HookRobotParameter;
                     if (key != null)
                     {
                         try
                         {
                             await Weixin.Work.AdvancedAPIs.Webhook.WebhookApi.SendTextAsync(key, message);
                         }
                         catch (Exception ex)
                         {
                             SenparcTrace.BaseExceptionLog(ex);
                         }
                     }
                     break;
                 default:
                     break;
             }
         };
    }
}
