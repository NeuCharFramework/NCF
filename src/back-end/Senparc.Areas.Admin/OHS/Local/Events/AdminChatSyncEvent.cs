/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatSyncEvent.cs
    文件功能描述：Admin Chat 安全同步通知事件

    创建标识：Senparc - 20260726

    修改标识：Senparc - 20260726
    修改描述：v0.1.0 增加后台 Admin Chat 同步服务以支持桌面管理交互

----------------------------------------------------------------*/

using System.Globalization;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Areas.Admin.OHS.Local.Events;

/// <summary>
/// 通知受权客户端重新读取 Admin Chat 数据；事件本身不携带聊天正文或认证信息。
/// </summary>
public sealed record AdminChatSyncEvent(
    int AdminUserId,
    int SessionId,
    string Action) : IntegrationEvent, IAuthorizedIntegrationSyncEvent
{
    public string Channel => "admin-chat";

    public string OwnerId => AdminUserId.ToString(CultureInfo.InvariantCulture);

    public string ResourceId => SessionId.ToString(CultureInfo.InvariantCulture);

    public string RequiredPolicy => "AdminOnly";

    public override string GetEventSummary()
    {
        return $"Admin Chat resource changed: SessionId={SessionId}, Action={Action}";
    }
}
