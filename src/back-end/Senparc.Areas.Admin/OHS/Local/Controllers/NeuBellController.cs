/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellController.cs
    文件功能描述：Admin Footer 服务器时间、纽铃快照与实时变更流

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.3.0 新增后台同步管理与可配置多语言页脚

    修改标识：Senparc - 20260804
    修改描述：v0.3.0 将后台同步功能统一更名为 NeuBell/纽铃

----------------------------------------------------------------*/

using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Areas.Admin.OHS.Local.Controllers;

[ApiController]
[Route("api/Senparc.Areas.Admin/neubell")]
[AdminAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public sealed class NeuBellController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NeuBellSnapshotService _snapshotService;
    private readonly NeuBellChangeNotifier _changeNotifier;

    public NeuBellController(
        NeuBellSnapshotService snapshotService,
        NeuBellChangeNotifier changeNotifier)
    {
        _snapshotService = snapshotService;
        _changeNotifier = changeNotifier;
    }

    [HttpGet("state")]
    public async Task<IActionResult> GetState(CancellationToken cancellationToken)
    {
        var context = CreateContext();
        var snapshots = await _snapshotService.GetSnapshotsAsync(context, cancellationToken).ConfigureAwait(false);
        return Ok(new
        {
            serverTime = DateTimeOffset.Now,
            providers = snapshots
        });
    }

    [HttpGet("events")]
    public async Task GetEvents(CancellationToken cancellationToken)
    {
        // EventSource 对 204 不会自动重连；无 Provider 时不保留长连接占用浏览器连接池。
        if (!await _snapshotService.HasAvailableProvidersAsync(cancellationToken).ConfigureAwait(false))
        {
            Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        try
        {
            await Response.WriteAsync(": connected\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

            await foreach (var providerId in _changeNotifier.SubscribeAsync(cancellationToken).ConfigureAwait(false))
            {
                var payload = JsonSerializer.Serialize(new { providerId }, JsonOptions);
                await Response.WriteAsync("event: neubell-changed\n", cancellationToken).ConfigureAwait(false);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken).ConfigureAwait(false);
                await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 浏览器关闭页面时正常结束流。
        }
    }

    private NeuBellRequestContext CreateContext()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.Identity?.Name
                     ?? "anonymous-admin";
        return new NeuBellRequestContext(userId);
    }
}
