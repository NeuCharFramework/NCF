/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharPivotBoardService.cs
    文件功能描述：NeuCharPivotBoard（Pivot 面板 / Provit Panel）服务：
    面板 CRUD、跨模块 Provit Block 校验与存储、AI Chat 创建或修改面板

    创建标识：Senparc - 20260901

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// Provit Block：面板中的一个功能块，引用某个 XNCF 模块 Pivot 的 Function
/// </summary>
public sealed class NeuCharPivotBoardBlock
{
    public string Key { get; set; }
    public string ModuleUid { get; set; }
    public string FunctionKey { get; set; }
    public string FunctionName { get; set; }
    public string Title { get; set; }
    public string Summary { get; set; }
    public string Accent { get; set; }
    public List<string> ExposedParameters { get; set; } = new();
}

/// <summary>
/// Pivot 面板（Provit Panel）服务
/// </summary>
public sealed class NeuCharPivotBoardService : BaseClientService<NeuCharPivotBoard>
{
    private static readonly HashSet<string> AllowedAccents = new(StringComparer.OrdinalIgnoreCase)
    {
        "blue", "green", "orange", "purple", "gray"
    };

    private readonly NeuCharPivotFunctionService _functionService;
    private readonly AdminChatAiService _aiService;
    private readonly AdminChatSessionService _sessionService;

    public NeuCharPivotBoardService(
        INeuCharPivotBoardRepository repository,
        IServiceProvider serviceProvider,
        NeuCharPivotFunctionService functionService,
        AdminChatAiService aiService,
        AdminChatSessionService sessionService)
        : base(repository, serviceProvider)
    {
        _functionService = functionService;
        _aiService = aiService;
        _sessionService = sessionService;
    }

    #region 面板 CRUD

    public async Task<NeuCharPivotBoard> GetEnabledBoardByPageKeyAsync(string pageKey)
    {
        var board = await GetObjectAsync(
            z => z.PageKey == NeuCharPivotBoard.NormalizePageKey(pageKey) && z.IsEnabled)
            .ConfigureAwait(false);
        return board;
    }

    public async Task<NeuCharPivotBoard> CreateBoardAsync(
        string name,
        string pageKey,
        string description,
        int columns,
        int adminUserId)
    {
        var board = new NeuCharPivotBoard(pageKey, name, adminUserId);
        board.UpdateInfo(name, description, columns, true);
        await SaveObjectAsync(board).ConfigureAwait(false);
        return board;
    }

    public async Task<NeuCharPivotBoard> UpdateBoardAsync(
        int boardId,
        string name,
        string pageKey,
        string description,
        int columns,
        bool isEnabled)
    {
        var board = await GetObjectAsync(z => z.Id == boardId).ConfigureAwait(false);
        if (board == null)
        {
            return null;
        }
        board.UpdateInfo(name, description, columns, isEnabled);
        if (!string.IsNullOrWhiteSpace(pageKey))
        {
            board.RebindPage(pageKey);
        }
        await SaveObjectAsync(board).ConfigureAwait(false);
        return board;
    }

    #endregion

    #region Provit Block

    /// <summary>
    /// 块选择器目录：全部 XNCF 模块 Pivot 中可见的 Function
    /// </summary>
    public async Task<List<NeuCharPivotBoardFunctionOption>> GetBlockCatalogAsync()
    {
        var functions = await _functionService.GetFullListAsync(
            z => z.Visible,
            z => z.Id,
            OrderingType.Ascending).ConfigureAwait(false);

        var registerList = XncfRegisterManager.RegisterList
            .Where(z => !string.IsNullOrWhiteSpace(z.Uid))
            .ToList();

        var groups = functions
            .GroupBy(z => z.ModuleUid, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new NeuCharPivotBoardFunctionOption
            {
                ModuleUid = group.Key,
                ModuleName = registerList.FirstOrDefault(z =>
                        string.Equals(z.Uid, group.Key, StringComparison.OrdinalIgnoreCase))?.Name
                    ?? group.Key,
                Functions = group
                    .OrderBy(z => z.Sort)
                    .Select(z => new NeuCharPivotBoardFunctionItem
                    {
                        FunctionKey = z.FunctionKey,
                        FunctionName = z.FunctionName,
                        Description = z.Description
                    })
                    .ToList()
            })
            .Where(group => group.Functions.Count > 0)
            .ToList();
        return groups;
    }

    /// <summary>
    /// 校验并保存面板的块列表（每个块必须对应真实存在的 Pivot Function）
    /// </summary>
    public async Task<(bool success, string message, NeuCharPivotBoard board)> SetBlocksAsync(
        int boardId,
        List<NeuCharPivotBoardBlock> blocks)
    {
        var board = await GetObjectAsync(z => z.Id == boardId).ConfigureAwait(false);
        if (board == null)
        {
            return (false, "面板不存在", null);
        }

        var allFunctions = await _functionService.GetFullListAsync(
            z => true,
            z => z.Id,
            OrderingType.Ascending).ConfigureAwait(false);
        var functionIndex = new Dictionary<string, NeuCharPivotFunction>(StringComparer.OrdinalIgnoreCase);
        foreach (var function in allFunctions)
        {
            functionIndex.TryAdd(
                $"{function.ModuleUid}|{function.FunctionKey}",
                function);
        }

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var validated = new List<NeuCharPivotBoardBlock>();
        foreach (var block in blocks ?? new List<NeuCharPivotBoardBlock>())
        {
            if (string.IsNullOrWhiteSpace(block.ModuleUid) || string.IsNullOrWhiteSpace(block.FunctionKey))
            {
                return (false, "存在缺少模块或 Function 的块", null);
            }
            if (!functionIndex.TryGetValue(
                $"{block.ModuleUid}|{block.FunctionKey}",
                out var function))
            {
                return (false, $"模块 {block.ModuleUid} 下不存在 Function：{block.FunctionKey}", null);
            }
            var key = string.IsNullOrWhiteSpace(block.Key)
                ? $"block-{Guid.NewGuid():N}".Substring(0, 13)
                : block.Key.Trim();
            if (!seenKeys.Add(key))
            {
                key = $"block-{Guid.NewGuid():N}".Substring(0, 13);
            }
            validated.Add(new NeuCharPivotBoardBlock
            {
                Key = key,
                ModuleUid = function.ModuleUid,
                FunctionKey = function.FunctionKey,
                FunctionName = function.FunctionName,
                Title = string.IsNullOrWhiteSpace(block.Title) ? block.FunctionKey : block.Title.Trim(),
                Summary = block.Summary?.Trim() ?? string.Empty,
                Accent = AllowedAccents.Contains(block.Accent ?? string.Empty) ? block.Accent : "blue",
                ExposedParameters = (block.ExposedParameters ?? new List<string>())
                    .Where(z => !string.IsNullOrWhiteSpace(z))
                    .ToList()
            });
        }

        board.SetBlocks(JsonConvert.SerializeObject(validated));
        await SaveObjectAsync(board).ConfigureAwait(false);
        return (true, "ok", board);
    }

    public static List<NeuCharPivotBoardBlock> DeserializeBlocks(string blocksJson)
    {
        if (string.IsNullOrWhiteSpace(blocksJson))
        {
            return new List<NeuCharPivotBoardBlock>();
        }
        try
        {
            return JsonConvert.DeserializeObject<List<NeuCharPivotBoardBlock>>(blocksJson)
                   ?? new List<NeuCharPivotBoardBlock>();
        }
        catch
        {
            return new List<NeuCharPivotBoardBlock>();
        }
    }

    #endregion

    #region AI Chat 创建 / 修改面板

    /// <summary>
    /// 通过 AI Chat 创建或修改面板。
    /// 指令示例：“为后台首页创建一个运维面板，加入主机监控和待办任务”
    /// 返回 AI 的说明文字与最新面板数据。
    /// </summary>
    public async Task<(bool success, string message, string aiReply, NeuCharPivotBoard board)> ApplyAiChangeAsync(
        int boardId,
        string instruction,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instruction))
        {
            return (false, "请输入面板需求描述", string.Empty, null);
        }

        var board = await GetObjectAsync(z => z.Id == boardId).ConfigureAwait(false);
        if (board == null)
        {
            return (false, "面板不存在", string.Empty, null);
        }

        var catalog = await GetBlockCatalogAsync().ConfigureAwait(false);
        var systemInstruction = BuildAiSystemInstruction(board, catalog);

        var session = await _sessionService.CreateSessionAsync(
            $"Pivot 面板 AI：{board.Name}", adminUserId).ConfigureAwait(false);

        try
        {
            var (response, _) = await _aiService.GenerateResponseAsync(
                    session.Id,
                    adminUserId,
                    instruction,
                    0,
                    null,
                    new AdminChatGenerationOptions
                    {
                        SystemInstructions = systemInstruction,
                        AllowFunctionInvocation = false,
                        MaxOutputTokens = 4000,
                        Temperature = 0.4f
                    }).ConfigureAwait(false);

            var parsed = ParseAiBoardJson(response, board, catalog);
            if (parsed == null)
            {
                return (false, "AI 返回的内容无法解析为面板配置，请换一种描述再试", response, board);
            }

            var (success, message, updated) = await SetBlocksAsync(
                board.Id,
                parsed.Blocks).ConfigureAwait(false);
            if (!success)
            {
                return (false, $"AI 生成的块未通过校验：{message}", response, board);
            }

            if (!string.IsNullOrWhiteSpace(parsed.Name) ||
                !string.IsNullOrWhiteSpace(parsed.Description) ||
                parsed.Columns > 0)
            {
                await UpdateBoardAsync(
                    board.Id,
                    parsed.Name ?? board.Name,
                    null,
                    parsed.Description ?? board.Description,
                    parsed.Columns,
                    board.IsEnabled).ConfigureAwait(false);
                updated = await GetObjectAsync(z => z.Id == board.Id).ConfigureAwait(false);
            }

            return (true, "ok", response, updated ?? board);
        }
        finally
        {
            // AI 专用临时会话，用完即删，避免污染用户会话列表
            try
            {
                await _sessionService.DeleteSessionAsync(session.Id, adminUserId).ConfigureAwait(false);
            }
            catch
            {
                // 忽略清理失败
            }
        }
    }

    private static string BuildAiSystemInstruction(
        NeuCharPivotBoard board,
        List<NeuCharPivotBoardFunctionOption> catalog)
    {
        var lines = new List<string>
        {
            "你是 NeuChar 后台的 Pivot 面板（Provit Panel）配置助手。",
            "用户会用自然语言描述希望面板包含哪些功能块（Provit Block），你需要基于【可用功能目录】生成或修改面板。",
            "只能使用目录中真实存在的 moduleUid 与 functionKey，严禁编造。",
            "只输出一个 JSON 对象，不要输出任何解释文字、markdown 代码块标记。",
            "JSON 结构：{\"name\":\"面板名称(可选)\",\"description\":\"面板说明(可选)\",\"columns\":列数(1-6,可选),\"blocks\":[{\"moduleUid\":\"...\",\"functionKey\":\"...\",\"title\":\"块标题\",\"summary\":\"一句话说明\",\"accent\":\"blue|green|orange|purple|gray\"}]}",
            "若用户只是要求修改/增删块，请输出修改后的完整 blocks 列表。"
        };

        if (board != null)
        {
            lines.Add($"当前面板：Name={board.Name}，PageKey={board.PageKey}，现有块：{board.BlocksJson}");
        }

        lines.Add("【可用功能目录】（moduleUid => functionKey: functionName）：");
        foreach (var group in catalog)
        {
            var functions = string.Join(", ", group.Functions.Select(z => $"{z.FunctionKey}: {z.FunctionName}"));
            lines.Add($"- {group.ModuleUid}（{group.ModuleName}）=> {functions}");
        }
        return string.Join("\n", lines);
    }

    private sealed class AiBoardProposal
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Columns { get; set; }
        public List<NeuCharPivotBoardBlock> Blocks { get; set; } = new();
    }

    private sealed class AiBoardProposalBlock
    {
        public string ModuleUid { get; set; }
        public string FunctionKey { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Accent { get; set; }
    }

    private static AiBoardProposal ParseAiBoardJson(
        string response,
        NeuCharPivotBoard board,
        List<NeuCharPivotBoardFunctionOption> catalog)
    {
        var json = ExtractJsonPayload(response);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        AiBoardProposal proposal;
        try
        {
            proposal = JsonConvert.DeserializeObject<AiBoardProposal>(json);
        }
        catch
        {
            return null;
        }
        if (proposal == null || proposal.Blocks == null || proposal.Blocks.Count == 0)
        {
            return null;
        }

        // 校验 AI 给出的 moduleUid/functionKey 必须在目录中真实存在
        var available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in catalog)
        {
            foreach (var function in group.Functions)
            {
                available.Add($"{group.ModuleUid}|{function.FunctionKey}");
            }
        }

        var blocks = new List<NeuCharPivotBoardBlock>();
        foreach (var block in proposal.Blocks)
        {
            if (string.IsNullOrWhiteSpace(block.ModuleUid) || string.IsNullOrWhiteSpace(block.FunctionKey))
            {
                continue;
            }
            if (!available.Contains($"{block.ModuleUid}|{block.FunctionKey}"))
            {
                continue;
            }
            blocks.Add(new NeuCharPivotBoardBlock
            {
                ModuleUid = block.ModuleUid,
                FunctionKey = block.FunctionKey,
                Title = block.Title,
                Summary = block.Summary,
                Accent = block.Accent
            });
        }
        if (blocks.Count == 0)
        {
            return null;
        }

        return new AiBoardProposal
        {
            Name = proposal.Name,
            Description = proposal.Description,
            Columns = proposal.Columns,
            Blocks = blocks
        };
    }

    private static string ExtractJsonPayload(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }
        var text = response.Trim();

        // 去除 markdown 代码块
        var fence = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
        if (fence.Success)
        {
            text = fence.Groups[1].Value.Trim();
        }

        // 截取第一个 { 到最后一个 } 之间的内容
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }
        return text.Substring(start, end - start + 1);
    }

    #endregion
}

/// <summary>
/// 块选择器目录项（模块）
/// </summary>
public sealed class NeuCharPivotBoardFunctionOption
{
    public string ModuleUid { get; set; }
    public string ModuleName { get; set; }
    public List<NeuCharPivotBoardFunctionItem> Functions { get; set; } = new();
}

/// <summary>
/// 块选择器目录项（Function）
/// </summary>
public sealed class NeuCharPivotBoardFunctionItem
{
    public string FunctionKey { get; set; }
    public string FunctionName { get; set; }
    public string Description { get; set; }
}
