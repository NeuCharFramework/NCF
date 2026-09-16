/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Board.cshtml.cs
    文件功能描述：Pivot 面板（Provit Panel）管理：面板 CRUD、
    跨 XNCF 模块 Provit Block 编排、AI Chat 创建或修改面板

    创建标识：Senparc - 20260901

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.WorkContext.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages.NeuCharPivot;

[IgnoreAuth]
[AdminAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
public class BoardModel(
    IServiceProvider serviceProvider,
    NeuCharPivotBoardService boardService,
    IAdminWorkContextProvider adminWorkContextProvider) : BaseAdminPageModel(serviceProvider)
{
    private readonly NeuCharPivotBoardService _boardService = boardService;
    private readonly IAdminWorkContextProvider _adminWorkContextProvider = adminWorkContextProvider;

    /// <summary>
    /// 默认 GET：渲染面板管理页（JSON 数据由 handler=Boards/Functions/ByPageKey 提供）
    /// </summary>
    public IActionResult OnGet()
    {
        return Page();
    }

    /// <summary>
    /// 面板列表（含反序列化后的块列表）
    /// </summary>
    public async Task<IActionResult> OnGetBoardsAsync()
    {
        var boards = await _boardService.GetFullListAsync(
            z => true, z => z.Id, OrderingType.Ascending).ConfigureAwait(false);
        return Ok(boards.Select(ToBoardDto));
    }

    /// <summary>
    /// 特定页面绑定的启用面板（首页等渲染用），无数据时 data 为 null
    /// </summary>
    public async Task<IActionResult> OnGetByPageKeyAsync([FromQuery] string pageKey)
    {
        var board = await _boardService.GetEnabledBoardByPageKeyAsync(pageKey).ConfigureAwait(false);
        return Ok(board == null ? null : ToBoardDto(board));
    }

    /// <summary>
    /// Provit Block 可选目录：全部 XNCF 模块 Pivot 中可见的 Function
    /// </summary>
    public async Task<IActionResult> OnGetFunctionsAsync()
    {
        var catalog = await _boardService.GetBlockCatalogAsync().ConfigureAwait(false);
        return Ok(catalog);
    }

    public async Task<IActionResult> OnPostCreateAsync([FromBody] BoardSaveRequest request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.PageKey))
        {
            return Ok(false, "面板名称与页面标识不能为空");
        }

        var adminUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        var board = await _boardService.CreateBoardAsync(
            request.Name, request.PageKey, request.Description, request.Columns, adminUserId)
            .ConfigureAwait(false);
        // 工厂默认启用，这里统一以请求中的 IsEnabled 为准
        board = await _boardService.UpdateBoardAsync(
            board.Id, board.Name, null, board.Description, board.Columns, request.IsEnabled)
            .ConfigureAwait(false);
        if (board == null)
        {
            return Ok(false, "创建失败");
        }
        return Ok(ToBoardDto(board));
    }

    public async Task<IActionResult> OnPostUpdateAsync([FromBody] BoardSaveRequest request)
    {
        if (request == null || request.Id <= 0 || string.IsNullOrWhiteSpace(request.Name))
        {
            return Ok(false, "面板信息无效");
        }

        var board = await _boardService.UpdateBoardAsync(
            request.Id, request.Name, request.PageKey, request.Description, request.Columns, request.IsEnabled)
            .ConfigureAwait(false);
        if (board == null)
        {
            return Ok(false, "面板不存在");
        }
        return Ok(ToBoardDto(board));
    }

    public async Task<IActionResult> OnPostDeleteAsync([FromBody] BoardIdRequest request)
    {
        if (request == null || request.Id <= 0)
        {
            return Ok(false, "面板 Id 无效");
        }
        await _boardService.DeleteObjectAsync(z => z.Id == request.Id).ConfigureAwait(false);
        return Ok(true);
    }

    public async Task<IActionResult> OnPostSetBlocksAsync([FromBody] BoardBlocksRequest request)
    {
        if (request == null || request.BoardId <= 0)
        {
            return Ok(false, "面板 Id 无效");
        }

        var (success, message, board) = await _boardService.SetBlocksAsync(request.BoardId, request.Blocks)
            .ConfigureAwait(false);
        if (!success)
        {
            return Ok(false, message);
        }
        return Ok(ToBoardDto(board));
    }

    /// <summary>
    /// AI Chat 创建 / 修改面板块（一次性指令，服务端校验后落库）
    /// </summary>
    public async Task<IActionResult> OnPostAiAsync([FromBody] BoardAiRequest request)
    {
        if (request == null || request.BoardId <= 0 || string.IsNullOrWhiteSpace(request.Instruction))
        {
            return Ok(false, "请输入面板需求描述");
        }

        var adminUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        var (success, message, aiReply, board) = await _boardService.ApplyAiChangeAsync(
            request.BoardId, request.Instruction, adminUserId, HttpContext.RequestAborted).ConfigureAwait(false);
        if (!success)
        {
            // 失败时也返回 AI 原始回复，便于前端提示与排查
            return Ok(new { aiReply = aiReply ?? string.Empty }, false, message);
        }
        return Ok(new { aiReply = aiReply ?? string.Empty, board = ToBoardDto(board) });
    }

    private static object ToBoardDto(NeuCharPivotBoard board)
    {
        return new
        {
            board.Id,
            board.PageKey,
            board.Name,
            board.Description,
            board.Columns,
            board.IsEnabled,
            board.AdminUserId,
            board.AddTime,
            board.LastUpdateTime,
            blocks = NeuCharPivotBoardService.DeserializeBlocks(board.BlocksJson)
        };
    }

    public sealed class BoardSaveRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PageKey { get; set; }
        public string Description { get; set; }
        public int Columns { get; set; } = 2;
        public bool IsEnabled { get; set; } = true;
    }

    public sealed class BoardIdRequest
    {
        public int Id { get; set; }
    }

    public sealed class BoardBlocksRequest
    {
        public int BoardId { get; set; }
        public List<NeuCharPivotBoardBlock> Blocks { get; set; } = new();
    }

    public sealed class BoardAiRequest
    {
        public int BoardId { get; set; }
        public string Instruction { get; set; }
    }
}
