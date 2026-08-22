/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellController.cs
    文件功能描述：Admin Footer 服务器时间、纽铃快照与实时变更流


    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.3.0 新增后台同步管理与可配置多语言页脚

    修改标识：Senparc - 20260804
    修改描述：v0.3.0 将后台同步功能统一更名为 NeuBell/纽铃

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Areas.Admin.OHS.Local.Controllers;

[ApiController]
[Route("api/Senparc.Areas.Admin/neubell")]
[AdminOrJwtAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public sealed class NeuBellController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NeuBellSnapshotService _snapshotService;
    private readonly NeuBellProviderCatalog _providerCatalog;
    private readonly NeuBellChangeNotifier _changeNotifier;

    public NeuBellController(
        NeuBellSnapshotService snapshotService,
        NeuBellProviderCatalog providerCatalog,
        NeuBellChangeNotifier changeNotifier)
    {
        _snapshotService = snapshotService;
        _providerCatalog = providerCatalog;
        _changeNotifier = changeNotifier;
    }

    [HttpGet("state")]
    public async Task<IActionResult> GetState(CancellationToken cancellationToken)
    {
        var context = CreateContext();
        var snapshots = await _snapshotService.GetSnapshotsAsync(context, cancellationToken).ConfigureAwait(false);
        var consumableProviderIds = (await _providerCatalog.GetAvailableProvidersAsync(cancellationToken).ConfigureAwait(false))
            .Where(provider => provider is INeuBellConsumableProvider)
            .Select(provider => provider.ProviderId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Ok(new
        {
            serverTime = DateTimeOffset.Now,
            providers = snapshots.Select(snapshot => new
            {
                snapshot.ProviderId,
                snapshot.ModuleUid,
                snapshot.DisplayName,
                snapshot.Icon,
                snapshot.DefaultVisible,
                snapshot.Items,
                canConsume = consumableProviderIds.Contains(snapshot.ProviderId)
            })
        });
    }

    /// <summary>
    /// 消费当前管理员可见的纽铃。Provider 未声明消费能力时返回冲突，调用方应仅导航到业务详情，
    /// 不能把“查看”当成“已处理”。
    /// </summary>
    [HttpPost("consume")]
    public async Task<IActionResult> Consume(
        [FromBody] NeuBellConsumeRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProviderId) ||
            (!request.ConsumeAll && string.IsNullOrWhiteSpace(request.ItemId)))
        {
            return BadRequest("纽铃消费请求缺少 Provider 或条目。");
        }

        var provider = (await _providerCatalog.GetAvailableProvidersAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item => string.Equals(item.ProviderId, request.ProviderId, StringComparison.OrdinalIgnoreCase));
        if (provider == null)
        {
            return NotFound("纽铃 Provider 不存在、未安装或未开启。");
        }
        if (provider is not INeuBellConsumableProvider consumableProvider)
        {
            return Conflict("该纽铃需要在其业务页面中处理，不能通过点击任务自动消费。");
        }

        var context = CreateContext();
        var consumedCount = request.ConsumeAll
            ? await consumableProvider.ConsumeAllAsync(context, cancellationToken).ConfigureAwait(false)
            : await consumableProvider.ConsumeItemAsync(context, request.ItemId, cancellationToken).ConfigureAwait(false);
        if (consumedCount > 0)
        {
            await _changeNotifier.NotifyChangedAsync(provider.ProviderId, cancellationToken).ConfigureAwait(false);
        }
        return Ok(new { consumedCount });
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

    public sealed class NeuBellConsumeRequest
    {
        public string? ProviderId { get; set; }
        public string? ItemId { get; set; }
        public bool ConsumeAll { get; set; }
    }
}
