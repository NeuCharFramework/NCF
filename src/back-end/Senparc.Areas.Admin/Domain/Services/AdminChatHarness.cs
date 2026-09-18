/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatHarness.cs
    文件功能描述：Admin Chat Harness（基于 Microsoft Agent Framework 的长任务执行器）

    创建标识：Senparc - 20260906

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

    修改标识：Senparc - 20260916
    修改描述：v0.9.0 增强 Admin Chat 取消与推理轨迹，并扩展 NeuBell WebHook 请求能力

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// Admin Chat 运行模式。
    /// </summary>
    public enum AdminChatMode
    {
        /// <summary>
        /// 普通单轮对话（默认）。
        /// </summary>
        Simple = 0,

        /// <summary>
        /// 长任务 Harness：多步骤自主执行（计划 → 调用工具 → 观察 → 继续），受最大步数与超时预算约束。
        /// </summary>
        Harness = 1
    }

    /// <summary>
    /// Harness 单步执行记录。
    /// </summary>
    public sealed class AdminChatHarnessStep
    {
        /// <summary>
        /// 步骤序号（从 1 开始）。
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 本步骤发送给模型的提示词（仅用于进度回调，不随响应序列化下发）。
        /// </summary>
        [JsonIgnore]
        public string Prompt { get; set; }

        /// <summary>
        /// 本步骤模型返回（已去除控制标记）的内容。
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// 是否为本轮结束步（命中完成标记）。
        /// </summary>
        public bool IsFinal { get; set; }

        /// <summary>
        /// 是否因超时/取消而终止。
        /// </summary>
        public bool TimedOut { get; set; }
    }

    /// <summary>
    /// Harness 执行结果。
    /// </summary>
    public sealed class AdminChatHarnessResult
    {
        /// <summary>
        /// 最终回复文本（最后一个有效步骤的内容）。
        /// </summary>
        public string FinalText { get; set; }

        /// <summary>
        /// 实际执行的步数。
        /// </summary>
        public int StepsUsed { get; set; }

        /// <summary>
        /// 是否主动完成（命中 <see cref="AdminChatHarnessExecutor.DoneToken"/>）。
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// 是否因达到最大步数而停止（未主动完成）。
        /// </summary>
        public bool ReachedMaxSteps { get; set; }

        /// <summary>
        /// 全部步骤记录（用于前端展示长任务执行过程）。
        /// </summary>
        public IReadOnlyList<AdminChatHarnessStep> Steps { get; set; } = Array.Empty<AdminChatHarnessStep>();

        /// <summary>
        /// Durable Trajectory identifier for native MAF Harness runs.
        /// </summary>
        public int TrajectoryId { get; set; }

        public int TrajectorySequence { get; set; }

        /// <summary>
        /// Native Harness trajectory status.
        /// </summary>
        public AdminChatTrajectoryStatus Status { get; set; }

        /// <summary>
        /// Native Harness trajectory events returned for the current request.
        /// </summary>
        public IReadOnlyList<AdminChatTrajectoryEventDto> TrajectoryEvents { get; set; } = Array.Empty<AdminChatTrajectoryEventDto>();

        /// <summary>
        /// Tool approvals waiting for explicit user confirmation.
        /// </summary>
        public IReadOnlyList<AdminChatApprovalRequestDto> PendingApprovals { get; set; } = Array.Empty<AdminChatApprovalRequestDto>();
    }

    /// <summary>
    /// Safe API projection of one stored Trajectory event.
    /// </summary>
    public sealed class AdminChatTrajectoryEventDto
    {
        public int Id { get; set; }
        public int Sequence { get; set; }
        public string EventType { get; set; }
        public string Source { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string PayloadJson { get; set; }
        public DateTime OccurredAt { get; set; }
        public string CorrelationId { get; set; }
        public bool IsReplayable { get; set; }

        public static AdminChatTrajectoryEventDto CreateFromEntity(AdminChatTrajectoryEvent entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new AdminChatTrajectoryEventDto
            {
                Id = entity.Id,
                Sequence = entity.Sequence,
                EventType = entity.EventType,
                Source = entity.Source,
                Name = entity.Name,
                Content = entity.Content,
                PayloadJson = entity.PayloadJson,
                OccurredAt = entity.OccurredAt,
                CorrelationId = entity.CorrelationId,
                IsReplayable = entity.IsReplayable
            };
        }

        /// <summary>
        /// Coalesces legacy consecutive assistant/reasoning text chunks for display and replay.
        /// New events already use one event per assistant phase.
        /// </summary>
        public static IReadOnlyList<AdminChatTrajectoryEventDto> CollapseAssistantTextChunks(
            IEnumerable<AdminChatTrajectoryEventDto> events)
        {
            var result = new List<AdminChatTrajectoryEventDto>();
            AdminChatTrajectoryEventDto current = null;

            foreach (var item in events ?? Enumerable.Empty<AdminChatTrajectoryEventDto>())
            {
                if (IsLegacyTextContentEvent(item))
                {
                    // Older runs persisted TextContent beside assistant.text for every chunk.
                    // Ignore it while keeping the current assistant phase open.
                    continue;
                }

                if (IsTextPhaseEvent(item))
                {
                    if (current != null
                        && string.Equals(current.EventType, item.EventType, StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrEmpty(current.CorrelationId)
                            && string.IsNullOrEmpty(item.CorrelationId)
                            || !string.IsNullOrEmpty(current.CorrelationId)
                            && string.Equals(current.CorrelationId, item.CorrelationId, StringComparison.Ordinal)))
                    {
                        current.Content = GetText(current) + GetText(item);
                        current.PayloadJson = item.PayloadJson ?? current.PayloadJson;
                        current.OccurredAt = item.OccurredAt;
                        continue;
                    }

                    current = new AdminChatTrajectoryEventDto
                    {
                        Id = item.Id,
                        Sequence = item.Sequence,
                        EventType = item.EventType,
                        Source = item.Source,
                        Name = item.Name,
                        Content = GetText(item),
                        PayloadJson = item.PayloadJson,
                        OccurredAt = item.OccurredAt,
                        CorrelationId = item.CorrelationId,
                        IsReplayable = item.IsReplayable
                    };
                    result.Add(current);
                    continue;
                }

                current = null;
                result.Add(item);
            }

            return result;
        }

        private static bool IsLegacyTextContentEvent(AdminChatTrajectoryEventDto item)
        {
            return item != null
                && (string.Equals(item.EventType, "TextContent", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.EventType, "Microsoft.Extensions.AI.TextContent", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.EventType, "text", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsTextPhaseEvent(AdminChatTrajectoryEventDto item)
        {
            return item != null
                && (string.Equals(item.EventType, "assistant.text", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.EventType, "TextReasoningContent", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.EventType, "Microsoft.Extensions.AI.TextReasoningContent", StringComparison.OrdinalIgnoreCase));
        }

        private static string GetText(AdminChatTrajectoryEventDto item)
        {
            if (!string.IsNullOrEmpty(item?.Content))
            {
                return item.Content;
            }

            if (string.IsNullOrEmpty(item?.PayloadJson))
            {
                return string.Empty;
            }

            try
            {
                using var payload = JsonDocument.Parse(item.PayloadJson);
                if (payload.RootElement.ValueKind == JsonValueKind.Object
                    && (payload.RootElement.TryGetProperty("Text", out var text)
                        || payload.RootElement.TryGetProperty("text", out text))
                    && text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                // Keep malformed legacy payloads unchanged.
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// A pending MAF tool approval rendered for the Admin Chat user.
    /// </summary>
    public sealed class AdminChatApprovalRequestDto
    {
        public string RequestId { get; set; }
        public string ToolCallId { get; set; }
        public string ToolName { get; set; }
        public string ArgumentsJson { get; set; }
    }

    public sealed class AdminChatLiveEvent
    {
        public int TrajectoryId { get; set; }
        public string Kind { get; set; }
        public string Text { get; set; }
        public AdminChatTrajectoryEventDto TrajectoryEvent { get; set; }
        public IReadOnlyList<AdminChatApprovalRequestDto> PendingApprovals { get; set; } = Array.Empty<AdminChatApprovalRequestDto>();
    }

    /// <summary>
    /// Safe API projection of a durable Harness Trajectory.
    /// </summary>
    public sealed class AdminChatTrajectoryDto
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public AdminChatMode Mode { get; set; }
        public AdminChatTrajectoryStatus Status { get; set; }
        public string Title { get; set; }
        public int? ParentTrajectoryId { get; set; }
        public int? ForkFromSequence { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int LastSequence { get; set; }
        public string ModelIdentifier { get; set; }

        public static AdminChatTrajectoryDto CreateFromEntity(AdminChatTrajectory entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new AdminChatTrajectoryDto
            {
                Id = entity.Id,
                SessionId = entity.SessionId,
                Mode = entity.Mode,
                Status = entity.Status,
                Title = entity.Title,
                ParentTrajectoryId = entity.ParentTrajectoryId,
                ForkFromSequence = entity.ForkFromSequence,
                StartedAt = entity.StartedAt,
                FinishedAt = entity.FinishedAt,
                LastSequence = entity.LastSequence,
                ModelIdentifier = entity.ModelIdentifier
            };
        }
    }

    /// <summary>
    /// 可测试的 MAF Harness 长任务执行器。
    /// <para>
    /// 按“步骤”驱动模型运行：每步调用 <see cref="TurnExecutor"/>（通常对应 MAF AIAgent 的一次运行），
    /// 读取模型输出中的控制标记来决定继续还是结束，直至命中完成标记、达到最大步数或超时。
    /// 本类与具体模型/框架无关，模型侧由 <see cref="TurnExecutor"/> 注入，因此便于单元测试。
    /// </para>
    /// </summary>
    public sealed class AdminChatHarnessExecutor
    {
        /// <summary>
        /// 继续标记：模型在本步末尾输出该标记表示任务未完成，需要继续下一步。
        /// </summary>
        public const string ContinueToken = "[[CONTINUE]]";

        /// <summary>
        /// 完成标记：模型在本步末尾输出该标记表示任务已完成，输出即为最终答案。
        /// </summary>
        public const string DoneToken = "[[DONE]]";

        /// <summary>
        /// 最大步骤数。
        /// </summary>
        public int MaxSteps { get; set; } = 8;

        /// <summary>
        /// 整轮 Harness 的超时时间。
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(120);

        /// <summary>
        /// 单步执行器：参数为 (步骤序号, 本步提示词, 取消令牌)，返回模型输出文本。
        /// </summary>
        public Func<int, string, CancellationToken, Task<string>> TurnExecutor { get; set; }

        /// <summary>
        /// 执行长任务，返回包含最终文本与全部步骤的结果。
        /// </summary>
        /// <param name="firstTurnPrompt">第一步提示词（包含任务与上下文）。</param>
        /// <param name="continuePrompt">后续每一步的继续提示词。</param>
        /// <param name="onStep">每完成一步的回调（用于前端进度展示）。</param>
        /// <param name="cancellationToken">外部取消令牌。</param>
        public async Task<AdminChatHarnessResult> RunAsync(
            string firstTurnPrompt,
            string continuePrompt,
            Action<AdminChatHarnessStep> onStep = null,
            CancellationToken cancellationToken = default)
        {
            if (TurnExecutor == null)
            {
                throw new InvalidOperationException("AdminChatHarnessExecutor.TurnExecutor 未设置。");
            }

            if (MaxSteps < 1)
            {
                MaxSteps = 1;
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(Timeout);
            var token = linkedCts.Token;

            var steps = new List<AdminChatHarnessStep>();
            string finalText = null;
            var completed = false;
            var used = 0;

            for (var i = 1; i <= MaxSteps; i++)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var prompt = i == 1 ? firstTurnPrompt : continuePrompt;
                string output;
                try
                {
                    output = await TurnExecutor(i, prompt, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }

                var trimmed = (output ?? string.Empty).Trim();
                var isDone = ContainsToken(trimmed, DoneToken);
                output = StripMarkers(string.IsNullOrWhiteSpace(trimmed) ? "(模型未返回有效内容)" : trimmed);
                used = i;
                var step = new AdminChatHarnessStep
                {
                    Index = i,
                    Prompt = prompt,
                    Output = output,
                    IsFinal = isDone,
                    TimedOut = token.IsCancellationRequested
                };
                steps.Add(step);
                onStep?.Invoke(step);
                finalText = output;

                if (isDone)
                {
                    completed = true;
                    break;
                }
            }

            if (finalText == null && steps.Count > 0)
            {
                finalText = steps[steps.Count - 1].Output;
            }

            return new AdminChatHarnessResult
            {
                FinalText = finalText ?? string.Empty,
                StepsUsed = used,
                Completed = completed,
                ReachedMaxSteps = !completed && used >= MaxSteps,
                Steps = steps
            };
        }

        /// <summary>
        /// 判断文本中是否包含指定控制标记（忽略大小写）。
        /// </summary>
        public static bool ContainsToken(string text, string token)
        {
            return !string.IsNullOrEmpty(text) && text.Contains(token, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 去除文本中的控制标记并规整多余空白，用于展示给最终用户。
        /// </summary>
        public static string StripMarkers(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var cleaned = text
                .Replace("[[CONTINUE]]", " ")
                .Replace("[[continue]]", " ")
                .Replace("[[DONE]]", " ")
                .Replace("[[done]]", " ");

            cleaned = Regex.Replace(cleaned, @"[ \t]+\n", "\n");
            cleaned = Regex.Replace(cleaned, @"\n{3,}", "\n\n");
            return cleaned.Trim();
        }

        /// <summary>
        /// 构造 Harness 系统指令中的“长任务执行协议”说明（供模型按步执行并在结尾输出控制标记）。
        /// </summary>
        public static string BuildProtocolInstructions()
        {
            return "\n\n## 长任务执行协议（Harness Mode）\n"
                + "你需要通过多步骤、自主执行的方式完成用户的长任务。每一步：\n"
                + "1) 先简要说明本步要做什么；\n"
                + "2) 如需要，调用可用的函数/工具获取数据或执行操作；\n"
                + "3) 给出本步小结。\n"
                + "在本步结尾必须单独一行输出控制标记：任务尚未完成输出 " + ContinueToken + "；任务已全部完成输出 " + DoneToken + " 并给出最终完整答案。\n";
        }

        /// <summary>
        /// 构造第一步提示词末尾的启动说明。
        /// </summary>
        public static string BuildStartTail()
        {
            return "\n\n请开始执行上述任务，并按“长任务执行协议”在结尾单独一行输出 " + ContinueToken + " 或 " + DoneToken + "。";
        }

        /// <summary>
        /// 构造后续每一步的继续提示词。
        /// </summary>
        public static string BuildContinuePrompt()
        {
            return "请继续执行任务：先概述上一步的进展与当前整体状态，再执行下一步。"
                + "在本步结尾单独一行输出 " + ContinueToken + "（继续）或 " + DoneToken + "（完成并给出最终完整答案）。";
        }
    }
}
