/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatWorkspaceAssetsController.cs
    文件功能描述：AdminChatWorkspaceAssetsController 相关定义与实现


    创建标识：Senparc - 20261024

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add bilingual contextual documentation help with iframe dialog and new-window access

----------------------------------------------------------------*/

using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Authorization;

namespace Senparc.Areas.Admin.OHS.Local.Controllers;

[ApiController]
[Route("api/Senparc.Areas.Admin/WorkspaceAssets")]
[AdminOrJwtAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminChatWorkspaceAssetsController : ControllerBase
{
    private static readonly SemaphoreSlim DownloadSlots = new(2, 2);
    private const long LargeAttachmentTransportLimit = 320L * 1024 * 1024;
    private readonly AdminChatWorkspaceAssetService _assets;
    private readonly WorkspaceAssetShareStore _shares;
    private readonly ILogger<AdminChatWorkspaceAssetsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public AdminChatWorkspaceAssetsController(
        AdminChatWorkspaceAssetService assets, WorkspaceAssetShareStore shares,
        ILogger<AdminChatWorkspaceAssetsController> logger, IWebHostEnvironment environment)
    {
        _assets = assets;
        _shares = shares;
        _logger = logger;
        _environment = environment;
    }

    [HttpGet("{sessionId:int}/preview")]
    public Task<IActionResult> Preview(int sessionId, [FromQuery] string path, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            var preview = await _assets.BuildPreviewAsync(session.WorkspaceId, path, cancellationToken);
            return Html(preview.Html);
        });

    [HttpGet("{sessionId:int}/attachments/capabilities")]
    public Task<IActionResult> AttachmentCapabilities(int sessionId, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            var attachments = HttpContext.RequestServices.GetRequiredService<AdminChatAttachmentService>();
            return Ok(new
            {
                maxFileBytes = attachments.LargeUploadLimit,
                inlineParseMaxFileBytes = attachments.UploadLimit,
                maxFiles = AdminChatAttachmentService.MaxAttachments,
                extensions = AdminChatAttachmentService.AllowedExtensions,
                notice = "小文件可本地解析；超过本地解析限制的文件会流式保存到 Workspace，不读取全文。请使用 Workspace/Python 分批处理，不会自动把大文件发送给 AI。"
            });
        });

    [HttpPost("{sessionId:int}/attachments")]
    [RequestSizeLimit(17 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 17 * 1024 * 1024)]
    public Task<IActionResult> UploadAttachment(int sessionId, IFormFile file,
        IFormFile visionPreview, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            var attachment = await HttpContext.RequestServices.GetRequiredService<AdminChatAttachmentService>()
                .UploadAsync(session, file, visionPreview, cancellationToken);
            return Ok(new { attachment });
        });

    [HttpPost("{sessionId:int}/attachments/large")]
    [RequestSizeLimit(LargeAttachmentTransportLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LargeAttachmentTransportLimit)]
    public Task<IActionResult> UploadLargeAttachment(int sessionId, IFormFile file,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            var attachment = await HttpContext.RequestServices.GetRequiredService<AdminChatAttachmentService>()
                .UploadLargeAsync(session, file, cancellationToken);
            return Ok(new { attachment });
        });

    [HttpPost("{sessionId:int}/attachments/delete")]
    [RequestSizeLimit(64 * 1024)]
    public Task<IActionResult> DeletePendingAttachment(int sessionId,
        [FromBody] AdminChatAttachmentInput request, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            var messages = await HttpContext.RequestServices.GetRequiredService<AdminChatMessageService>()
                .GetSessionMessagesAsync(sessionId);
            await HttpContext.RequestServices.GetRequiredService<AdminChatAttachmentService>()
                .DeletePendingAsync(session, request, messages.messages, cancellationToken);
            return Ok(new { deleted = true });
        });

    [HttpGet("{sessionId:int}/download")]
    public Task<IActionResult> Download(int sessionId, [FromQuery] string path = "",
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            if (!await DownloadSlots.WaitAsync(0, cancellationToken))
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "下载任务繁忙，请稍后重试。" });
            try
            {
                var download = await _assets.DownloadAsync(session.WorkspaceId, path, cancellationToken);
                Response.Headers["X-Content-Type-Options"] = "nosniff";
                return File(download.Bytes, "application/octet-stream", download.FileName);
            }
            finally { DownloadSlots.Release(); }
        });

    [HttpGet("{sessionId:int}/shares")]
    public Task<IActionResult> ListShares(int sessionId, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            EnsureShareTransport();
            var shares = await _shares.ListAsync(GetOwnerId(), sessionId, session.WorkspaceId, cancellationToken);
            return Ok(new { shares = System.Linq.Enumerable.Select(shares, ShareDto) });
        });

    [HttpPost("{sessionId:int}/shares")]
    [RequestSizeLimit(4096)]
    public Task<IActionResult> CreateShare(int sessionId, [FromBody] CreateWorkspaceAssetShare request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            EnsureShareTransport();
            if (request.ExpiresInHours is not (1 or 24 or 168))
                throw new ArgumentException("分享有效期只能是 1 小时、24 小时或 7 天。");
            var preview = await _assets.BuildPreviewAsync(session.WorkspaceId, request.Path, cancellationToken);
            var share = await _shares.CreateAsync(GetOwnerId(), sessionId, session.WorkspaceId,
                request.Path, preview.Html, request.ExpiresInHours, cancellationToken);
            return Ok(new { share = ShareDto(share), warnings = preview.Warnings });
        });

    [HttpPost("{sessionId:int}/shares/{shareId}/revoke")]
    public Task<IActionResult> RevokeShare(int sessionId, string shareId, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            return await _shares.RevokeAsync(GetOwnerId(), sessionId, session.WorkspaceId, shareId, cancellationToken)
                ? Ok(new { revoked = true })
                : NotFound(new { message = "分享不存在或已失效。" });
        });

    [AllowAnonymous]
    [HttpGet("shared/{id}/{token}")]
    public Task<IActionResult> Shared(string id, string token, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            Response.Headers["Referrer-Policy"] = "no-referrer";
            Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
            EnsureShareTransport();
            if (!_shares.TryAcquirePublicRequest(HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"))
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "访问过于频繁，请稍后重试。" });
            var share = await _shares.ReadPublicAsync(id, token, cancellationToken);
            if (share == null || !await _assets.IsShareWorkspaceAvailableAsync(share, cancellationToken))
                return NotFound(new { message = "分享不存在、已撤销或已过期。" });
            return Html(share.Html);
        });

    private object ShareDto(WorkspaceAssetShare share) => new
    {
        share.Id, share.Path, share.CreatedAt, share.ExpiresAt,
        url = $"{Request.PathBase}/api/Senparc.Areas.Admin/WorkspaceAssets/shared/{share.Id}/{share.Token}"
    };

    private int GetOwnerId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    private void EnsureShareTransport()
    {
        if (Request.IsHttps) return;
        var loopbackHost = string.Equals(Request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || IPAddress.TryParse(Request.Host.Host, out var host) && IPAddress.IsLoopback(host);
        if (_environment.IsDevelopment() && loopbackHost
            && HttpContext.Connection.RemoteIpAddress is { } remote && IPAddress.IsLoopback(remote))
            return;
        throw new InvalidOperationException("匿名分享需要 HTTPS，本机回环地址调试除外；反向代理应正确转发 HTTPS 协议。");
    }

    private FileContentResult Html(string html)
    {
        Response.Headers["Content-Security-Policy"] = WorkspaceAssetPreviewBuilder.ContentSecurityPolicy;
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        Response.Headers["X-Frame-Options"] = "DENY";
        Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
        Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        Response.Headers.CacheControl = "no-store";
        return File(Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8");
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (UnauthorizedAccessException error)
        {
            _logger.LogWarning("Workspace asset access denied: {Reason}", error.Message);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "无权访问此 Workspace。" });
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
        {
            return NotFound(new { message = "Workspace、文件或分享不存在。" });
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or InvalidDataException or NotSupportedException or System.Xml.XmlException)
        {
            _logger.LogWarning("Workspace asset request rejected: {Reason}", error.Message);
            return BadRequest(new { message = error.Message });
        }
        catch (IOException error)
        {
            _logger.LogError(error, "Workspace asset operation failed.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Workspace 文件操作失败，请检查服务器日志。" });
        }
    }

    public sealed class CreateWorkspaceAssetShare
    {
        public string Path { get; set; } = "";
        public int ExpiresInHours { get; set; } = 24;
    }
}
