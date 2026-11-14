/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatPromptReferenceAppService.cs
    文件功能描述：后台聊天 PromptCode 评分应用服务


    创建标识：Senparc - 20261104

    修改标识：Senparc - 20261105
    修改描述：v0.12.26 提供 PromptCode 的人工与 AI 评分接口

----------------------------------------------------------------*/
using System;
using System.Threading.Tasks;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Xncf.PromptRange.Abstractions.References;

namespace Senparc.Areas.Admin.OHS.Local.AppService;

[AdminOrJwtAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public sealed class AdminChatPromptReferenceAppService : LocalAppServiceBase
{
    public AdminChatPromptReferenceAppService(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<PromptReferenceInfo>> ScoreWithAIAsync(AdminChatPromptScoreRequest request)
    {
        return this.GetResponseAsync<PromptReferenceInfo>(async (response, logger) =>
        {
            return await GetService<AdminChatMessageService>().ScorePromptReferenceWithAIAsync(
                GetCurrentAdminUserInfoId(), request.MessageId, request.PromptItemId);
        });
    }
}

public sealed class AdminChatPromptScoreRequest
{
    public int MessageId { get; set; }
    public int PromptItemId { get; set; }
}
