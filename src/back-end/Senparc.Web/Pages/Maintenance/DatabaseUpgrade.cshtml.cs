/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DatabaseUpgrade.cshtml.cs
    文件功能描述：数据库升级维护页

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.36.0 展示数据库运行状态并返回维护状态码

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Web.Infrastructure.Database;

namespace Senparc.Web.Pages.Maintenance;

public sealed class DatabaseUpgradeModel : PageModel
{
    private readonly DatabaseRuntimeStateStore _stateStore;

    public DatabaseUpgradeModel(DatabaseRuntimeStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public DatabaseRuntimeState State { get; private set; }

    public void OnGet()
    {
        State = _stateStore.Current;
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    }
}
