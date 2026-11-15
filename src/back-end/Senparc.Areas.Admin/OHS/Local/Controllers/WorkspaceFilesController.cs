/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceFilesController.cs
    文件功能描述：WorkspaceFilesController 相关定义与实现


    创建标识：Senparc - 20261102

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add streamed large-file Workspace uploads with tenant-aware ownership checks

----------------------------------------------------------------*/

using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Senparc.Areas.Admin;
using Senparc.Xncf.Workspace;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Areas.Admin.OHS.Local.Controllers;

[ApiController]
[Route("api/Senparc.Areas.Admin/WorkspaceFiles")]
[AdminOrJwtAuthorize("AdminSuperAdmin")]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class WorkspaceFilesController : ControllerBase
{
    private const long TransportLimitBytes = 320L * 1024 * 1024;
    private readonly IWorkspaceStorage _storage;
    private readonly WorkspaceOptions _options;

    public WorkspaceFilesController(
        IWorkspaceStorage storage,
        IOptions<WorkspaceOptions> options)
    {
        _storage = storage;
        _options = options.Value;
    }

    [HttpGet("capabilities")]
    public IActionResult Capabilities()
    {
        return Ok(new
        {
            maxFileBytes = _options.MaxUploadFileBytes,
            workspaceQuotaBytes = _options.MaxWorkspaceBytes,
            targetDirectory = "input"
        });
    }

    [HttpPost("{workspaceId}/upload")]
    [RequestSizeLimit(TransportLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = TransportLimitBytes)]
    public async Task<IActionResult> Upload(
        string workspaceId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentOwnerId();
        var workspace = await _storage.GetAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        if (workspace == null || workspace.OwnerId != ownerId)
        {
            return NotFound(new { message = "Workspace 不存在或不属于当前管理员。" });
        }

        if (file == null || file.Length <= 0)
        {
            return BadRequest(new { message = "上传文件不能为空。" });
        }

        if (file.Length > _options.MaxUploadFileBytes)
        {
            return BadRequest(new
            {
                message = $"单个文件不能超过 {_options.MaxUploadFileBytes} 字节。"
            });
        }

        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (fileName.Length == 0
            || fileName.Length > 200
            || fileName.Any(char.IsControl)
            || fileName.Contains(':'))
        {
            return BadRequest(new { message = "文件名无效或超过 200 个字符。" });
        }

        var relativePath = $"input/{fileName}";
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _storage.WriteFileStreamAsync(
                workspaceId,
                new WorkspaceStreamWriteRequest(
                    relativePath,
                    Overwrite: true,
                    ContentLength: file.Length),
                stream,
                cancellationToken).ConfigureAwait(false);
            return Ok(new
            {
                file = result.Metadata,
                message = "文件已上传到 Workspace 的 input 目录。"
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private int GetCurrentOwnerId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var ownerId) || ownerId <= 0)
        {
            throw new UnauthorizedAccessException("当前管理员身份无效。");
        }

        return ownerId;
    }
}
