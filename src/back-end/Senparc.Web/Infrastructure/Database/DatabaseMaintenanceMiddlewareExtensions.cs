/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DatabaseMaintenanceMiddlewareExtensions.cs
    文件功能描述：数据库未就绪时隔离业务请求

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.35.0 新增数据库升级维护流程与多平台下载入口

----------------------------------------------------------------*/

namespace Senparc.Web.Infrastructure.Database;

public static class DatabaseMaintenanceMiddlewareExtensions
{
    public const string MaintenancePath = "/Maintenance/DatabaseUpgrade";

    public static IApplicationBuilder UseDatabaseMaintenanceMode(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var state = context.RequestServices.GetRequiredService<DatabaseRuntimeStateStore>().Current;
            if (state.Status is DatabaseRuntimeStatus.Ready or DatabaseRuntimeStatus.Uninitialized
                || context.Request.Path.StartsWithSegments(MaintenancePath)
                // 维护状态仍表示进程存活；/health 则继续返回 503，供编排系统停止业务流量。
                || context.Request.Path.StartsWithSegments("/alive"))
            {
                await next().ConfigureAwait(false);
                return;
            }

            var acceptsHtml = context.Request.Headers.Accept.Any(value =>
                value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
            if (HttpMethods.IsGet(context.Request.Method) && acceptsHtml)
            {
                context.Response.Redirect(MaintenancePath);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "database_not_ready",
                status = state.Status.ToString(),
                message = state.Message,
                maintenanceUrl = MaintenancePath
            }).ConfigureAwait(false);
        });
    }
}
