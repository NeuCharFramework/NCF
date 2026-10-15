/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawAdminBinding.cs
    文件功能描述：WeixinClawAdminBinding.cs implementation and project behavior.


    创建标识：Senparc - 20260926

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
/// 将一个个人微信 Claw 对话绑定到一个 Admin 管理员和 Admin Chat 会话。
/// 此表属于 Admin 上层，不属于 WeixinManager 通道层。
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(WeixinClawAdminBinding))]
[Serializable]
public sealed class WeixinClawAdminBinding : EntityBase<int>
{
    [Required]
    public int AccountId { get; private set; }

    [Required, MaxLength(300)]
    public string FromUserId { get; private set; }

    [MaxLength(300)]
    public string GroupId { get; private set; }

    [Required]
    public int AdminUserId { get; private set; }

    public int? AdminChatSessionId { get; private set; }
    public int? LastTrajectoryId { get; private set; }
    public int LastTrajectorySequence { get; private set; }
    public int AiModelId { get; private set; }
    public int? WorkflowId { get; private set; }
    public int Mode { get; private set; }
    public bool Enabled { get; private set; }
    public bool EnableNeuBell { get; private set; }
    public bool EnableWorkflow { get; private set; }

    [MaxLength(1500)]
    public string ContextToken { get; private set; }

    [MaxLength(2000)]
    public string NeuBellPushStateJson { get; private set; }

    [MaxLength(300)]
    public string PendingApprovalRequestId { get; private set; }

    [MaxLength(300)]
    public string PendingApprovalToolCallId { get; private set; }

    [MaxLength(300)]
    public string PendingApprovalToolName { get; private set; }

    public string PendingApprovalArgumentsJson { get; private set; }

    public DateTime? LastMessageAt { get; private set; }

    private WeixinClawAdminBinding()
    {
    }

    public WeixinClawAdminBinding(
        int accountId,
        string fromUserId,
        string groupId,
        int adminUserId,
        bool enableNeuBell,
        bool enableWorkflow)
    {
        AccountId = accountId;
        FromUserId = fromUserId?.Trim() ?? string.Empty;
        GroupId = groupId?.Trim() ?? string.Empty;
        AdminUserId = adminUserId;
        Mode = 0;
        Enabled = true;
        EnableNeuBell = enableNeuBell;
        EnableWorkflow = enableWorkflow;
        AiModelId = 0;
    }

    public void SetChatSession(int sessionId)
    {
        AdminChatSessionId = sessionId > 0 ? sessionId : null;
        SetUpdateTime();
    }

    public void SetConversationState(
        string contextToken,
        int? trajectoryId,
        int trajectorySequence,
        DateTime messageAt)
    {
        ContextToken = contextToken;
        LastTrajectoryId = trajectoryId;
        LastTrajectorySequence = trajectorySequence;
        LastMessageAt = messageAt;
        SetUpdateTime();
    }

    public void SetContextToken(string contextToken, DateTime messageAt)
    {
        ContextToken = string.IsNullOrWhiteSpace(contextToken) ? null : contextToken;
        LastMessageAt = messageAt;
        SetUpdateTime();
    }

    public bool HasNeuBellPush(string providerId, string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(fingerprint))
        {
            return false;
        }

        var state = ReadNeuBellPushState();
        return state.TryGetValue(providerId, out var previous)
            && string.Equals(previous, fingerprint, StringComparison.Ordinal);
    }

    public void MarkNeuBellPushed(string providerId, string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(fingerprint))
        {
            return;
        }

        var state = ReadNeuBellPushState();
        state[providerId] = fingerprint;
        NeuBellPushStateJson = JsonSerializer.Serialize(state);
        SetUpdateTime();
    }

    private Dictionary<string, string> ReadNeuBellPushState()
    {
        if (string.IsNullOrWhiteSpace(NeuBellPushStateJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var state = JsonSerializer.Deserialize<Dictionary<string, string>>(NeuBellPushStateJson);
            return state == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(state, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public void SetPendingApproval(
        string requestId,
        string toolCallId,
        string toolName,
        string argumentsJson)
    {
        PendingApprovalRequestId = requestId;
        PendingApprovalToolCallId = toolCallId;
        PendingApprovalToolName = toolName;
        PendingApprovalArgumentsJson = argumentsJson;
        SetUpdateTime();
    }

    public void ClearPendingApproval()
    {
        PendingApprovalRequestId = null;
        PendingApprovalToolCallId = null;
        PendingApprovalToolName = null;
        PendingApprovalArgumentsJson = null;
        SetUpdateTime();
    }

    public void SetRouting(
        int mode,
        int aiModelId,
        int? workflowId,
        bool enableNeuBell,
        bool enableWorkflow)
    {
        Mode = mode;
        AiModelId = aiModelId;
        WorkflowId = workflowId;
        EnableNeuBell = enableNeuBell;
        EnableWorkflow = enableWorkflow;
        SetUpdateTime();
    }

    public void Disable()
    {
        Enabled = false;
        SetUpdateTime();
    }
}
