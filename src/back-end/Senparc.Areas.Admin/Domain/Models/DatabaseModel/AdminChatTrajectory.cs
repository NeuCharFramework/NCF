/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatTrajectory.cs
    文件功能描述：AdminChatTrajectory 相关功能实现


    创建标识：Senparc - 20260915

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
/// A durable Admin Chat Harness run. The event rows form an append-only trajectory.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(AdminChatTrajectory))]
[Serializable]
public class AdminChatTrajectory : EntityBase<int>
{
    [Required]
    public int SessionId { get; private set; }

    [Required]
    public int UserId { get; private set; }

    public int? ParentTrajectoryId { get; private set; }

    public int? ForkFromSequence { get; private set; }

    [Required, MaxLength(150)]
    public string Title { get; private set; }

    [Required]
    public AdminChatMode Mode { get; private set; }

    [Required]
    public AdminChatTrajectoryStatus Status { get; private set; }

    [Required]
    public DateTime StartedAt { get; private set; }

    public DateTime? FinishedAt { get; private set; }

    [Required]
    public int LastSequence { get; private set; }

    [MaxLength(100)]
    public string ModelIdentifier { get; private set; }

    public string SessionStateJson { get; private set; }

    public string LastError { get; private set; }

    [ForeignKey(nameof(SessionId))]
    public virtual AdminChatSession Session { get; private set; }

    public virtual ICollection<AdminChatTrajectoryEvent> Events { get; private set; } = new List<AdminChatTrajectoryEvent>();

    private AdminChatTrajectory() { }

    public AdminChatTrajectory(
        int sessionId,
        int userId,
        string title,
        AdminChatMode mode,
        string modelIdentifier = null,
        int? parentTrajectoryId = null,
        int? forkFromSequence = null)
    {
        SessionId = sessionId;
        UserId = userId;
        Title = string.IsNullOrWhiteSpace(title)
            ? "Harness 任务"
            : title.Length > 150 ? title.Substring(0, 150) : title;
        Mode = mode;
        Status = AdminChatTrajectoryStatus.Running;
        StartedAt = DateTime.Now;
        LastSequence = 0;
        ModelIdentifier = modelIdentifier;
        ParentTrajectoryId = parentTrajectoryId;
        ForkFromSequence = forkFromSequence;
    }

    public int NextSequence() => LastSequence + 1;

    public void MarkSequence(int sequence)
    {
        if (sequence > LastSequence)
        {
            LastSequence = sequence;
        }
        base.SetUpdateTime();
    }

    public void SetSessionState(string sessionStateJson)
    {
        SessionStateJson = sessionStateJson;
        base.SetUpdateTime();
    }

    public void Complete()
    {
        Status = AdminChatTrajectoryStatus.Completed;
        FinishedAt = DateTime.Now;
        base.SetUpdateTime();
    }

    public void WaitForApproval()
    {
        Status = AdminChatTrajectoryStatus.WaitingForApproval;
        base.SetUpdateTime();
    }

    public void Fail(string error)
    {
        Status = AdminChatTrajectoryStatus.Failed;
        LastError = error;
        FinishedAt = DateTime.Now;
        base.SetUpdateTime();
    }

    public void Cancel()
    {
        Status = AdminChatTrajectoryStatus.Cancelled;
        FinishedAt = DateTime.Now;
        base.SetUpdateTime();
    }
}

public enum AdminChatTrajectoryStatus
{
    Running = 0,
    WaitingForApproval = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

/// <summary>
/// One append-only event in an Admin Chat Harness trajectory.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(AdminChatTrajectoryEvent))]
[Serializable]
public class AdminChatTrajectoryEvent : EntityBase<int>
{
    [Required]
    public int TrajectoryId { get; private set; }

    [Required]
    public int Sequence { get; private set; }

    [Required, MaxLength(50)]
    public string EventType { get; private set; }

    [MaxLength(100)]
    public string Source { get; private set; }

    [MaxLength(200)]
    public string Name { get; private set; }

    public string Content { get; private set; }

    public string PayloadJson { get; private set; }

    [Required]
    public DateTime OccurredAt { get; private set; }

    [MaxLength(100)]
    public string CorrelationId { get; private set; }

    [Required]
    public bool IsReplayable { get; private set; }

    [ForeignKey(nameof(TrajectoryId))]
    public virtual AdminChatTrajectory Trajectory { get; private set; }

    private AdminChatTrajectoryEvent() { }

    public AdminChatTrajectoryEvent(
        int trajectoryId,
        int sequence,
        string eventType,
        string source,
        string name,
        string content,
        string payloadJson,
        string correlationId = null,
        bool isReplayable = true)
    {
        TrajectoryId = trajectoryId;
        Sequence = sequence;
        EventType = eventType ?? "system";
        Source = source;
        Name = name;
        Content = content;
        PayloadJson = payloadJson;
        OccurredAt = DateTime.Now;
        CorrelationId = correlationId;
        IsReplayable = isReplayable;
    }
}
