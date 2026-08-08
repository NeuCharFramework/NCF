/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DatabaseUpgrade.cshtml.cs
    文件功能描述：数据库升级维护页

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.35.0 新增数据库升级维护流程与多平台下载入口

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Senparc.Web.Pages.Maintenance;

public sealed class DatabaseUpgradeModel : PageModel
{
    public void OnGet()
    {
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Append("X-Robots-Tag", "noindex, nofollow");
    }
}
