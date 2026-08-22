/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharPivotModels.cs
    文件功能描述：NeuCharPivot、Loop Task、Workflow 与执行日志系统实体


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(NeuCharPivotConfiguration))]
[Serializable]
public class NeuCharPivotConfiguration : EntityBase<int>
{
    [Required, MaxLength(100)]
    public string ModuleUid { get; private set; }

    [Required, MaxLength(200)]
    public string Name { get; private set; }

    public string UserRequirement { get; private set; }

    public string LayoutSchemaJson { get; private set; }

    public int AiModelId { get; private set; }

    public int AdminUserId { get; private set; }

    public int? ChatSessionId { get; private set; }

    public int Revision { get; private set; }

    public DateTime? LastGeneratedAt { get; private set; }

    public string LastError { get; private set; }

    private NeuCharPivotConfiguration() { }

    public NeuCharPivotConfiguration(string moduleUid, string name, int adminUserId)
    {
        ModuleUid = moduleUid;
        Name = name;
        AdminUserId = adminUserId;
        LayoutSchemaJson = string.Empty;
        UserRequirement = string.Empty;
        Revision = 0;
    }

    public void ApplyGeneratedLayout(
        string name,
        string requirement,
        string layoutSchemaJson,
        int aiModelId,
        int adminUserId,
        int? chatSessionId)
    {
        Name = string.IsNullOrWhiteSpace(name) ? Name : name.Trim();
        UserRequirement = requirement?.Trim() ?? string.Empty;
        LayoutSchemaJson = layoutSchemaJson ?? string.Empty;
        AiModelId = aiModelId;
        AdminUserId = adminUserId;
        ChatSessionId = chatSessionId;
        Revision++;
        LastGeneratedAt = DateTime.UtcNow;
        LastError = null;
        SetUpdateTime();
    }

    public void RecordError(string error)
    {
        LastError = Truncate(error, 4000);
        SetUpdateTime();
    }

    private static string Truncate(string value, int length) =>
        string.IsNullOrEmpty(value) || value.Length <= length ? value : value[..length];
}

[Table(Register.DATABASE_PREFIX + nameof(NeuCharPivotFunction))]
[Serializable]
public class NeuCharPivotFunction : EntityBase<int>
{
    public int PivotId { get; private set; }

    [Required, MaxLength(100)]
    public string ModuleUid { get; private set; }

    [Required, MaxLength(200)]
    public string FunctionKey { get; private set; }

    [Required, MaxLength(200)]
    public string FunctionName { get; private set; }

    public string Description { get; private set; }

    public string UiSchemaJson { get; private set; }

    public string DefaultParametersJson { get; private set; }

    [MaxLength(100)]
    public string ModuleVersion { get; private set; }

    public int Sort { get; private set; }

    public bool Visible { get; private set; }

    private NeuCharPivotFunction() { }

    public NeuCharPivotFunction(
        int pivotId,
        string moduleUid,
        string functionKey,
        string functionName,
        string description)
    {
        PivotId = pivotId;
        ModuleUid = moduleUid;
        FunctionKey = functionKey;
        FunctionName = functionName;
        Description = description;
        UiSchemaJson = "{}";
        DefaultParametersJson = "{}";
        Visible = true;
    }

    public void Update(
        string functionName,
        string description,
        string uiSchemaJson,
        string defaultParametersJson,
        string moduleVersion,
        int sort,
        bool visible)
    {
        FunctionName = functionName;
        Description = description;
        UiSchemaJson = uiSchemaJson ?? "{}";
        DefaultParametersJson = defaultParametersJson ?? "{}";
        ModuleVersion = moduleVersion;
        Sort = sort;
        Visible = visible;
        SetUpdateTime();
    }
}

[Table(Register.DATABASE_PREFIX + nameof(NeuCharPivotLoopTask))]
[Serializable]
public class NeuCharPivotLoopTask : EntityBase<int>
{
    public int FunctionId { get; private set; }

    public int AdminUserId { get; private set; }

    public int IntervalSeconds { get; private set; }

    public string ParametersJson { get; private set; }

    public bool Enabled { get; private set; }

    public bool UseNeuBell { get; private set; }

    public DateTime? NextRunAt { get; private set; }

    public DateTime? LastRunAt { get; private set; }

    public bool? LastSucceeded { get; private set; }

    public int ConsecutiveFailures { get; private set; }

    public string LastError { get; private set; }

    private NeuCharPivotLoopTask() { }

    public NeuCharPivotLoopTask(int functionId, int adminUserId)
    {
        FunctionId = functionId;
        AdminUserId = adminUserId;
        IntervalSeconds = 300;
        ParametersJson = "{}";
    }

    public void Configure(int intervalSeconds, string parametersJson, bool enabled, bool useNeuBell)
    {
        IntervalSeconds = Math.Clamp(intervalSeconds, 60, 31_536_000);
        ParametersJson = parametersJson ?? "{}";
        Enabled = enabled;
        UseNeuBell = useNeuBell;
        NextRunAt = enabled ? DateTime.UtcNow.AddSeconds(IntervalSeconds) : null;
        if (!enabled)
        {
            ConsecutiveFailures = 0;
        }
        SetUpdateTime();
    }

    public void MarkStarted()
    {
        LastRunAt = DateTime.UtcNow;
        NextRunAt = LastRunAt.Value.AddSeconds(Math.Max(60, IntervalSeconds));
        SetUpdateTime();
    }

    public void MarkCompleted(bool succeeded, string error)
    {
        LastSucceeded = succeeded;
        ConsecutiveFailures = succeeded ? 0 : ConsecutiveFailures + 1;
        LastError = succeeded ? null : Truncate(error, 4000);
        SetUpdateTime();
    }

    public void DisableForError(string error)
    {
        Enabled = false;
        NextRunAt = null;
        LastSucceeded = false;
        ConsecutiveFailures++;
        LastError = Truncate(error, 4000);
        SetUpdateTime();
    }

    private static string Truncate(string value, int length) =>
        string.IsNullOrEmpty(value) || value.Length <= length ? value : value[..length];
}

[Table(Register.DATABASE_PREFIX + nameof(NeuCharExecutionLog))]
[Serializable]
public class NeuCharExecutionLog : EntityBase<int>
{
    [Required, MaxLength(40)]
    public string SourceType { get; private set; }

    public int SourceId { get; private set; }

    [MaxLength(100)]
    public string ModuleUid { get; private set; }

    [MaxLength(200)]
    public string FunctionKey { get; private set; }

    [MaxLength(200)]
    public string DisplayName { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime? FinishedAt { get; private set; }

    public bool? Succeeded { get; private set; }

    public string ResultSummary { get; private set; }

    public string Error { get; private set; }

    [MaxLength(100)]
    public string CorrelationId { get; private set; }

    private NeuCharExecutionLog() { }

    public NeuCharExecutionLog(
        string sourceType,
        int sourceId,
        string moduleUid,
        string functionKey,
        string displayName,
        string correlationId)
    {
        SourceType = sourceType;
        SourceId = sourceId;
        ModuleUid = moduleUid;
        FunctionKey = functionKey;
        DisplayName = displayName;
        CorrelationId = correlationId;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(bool succeeded, string resultSummary, string error)
    {
        FinishedAt = DateTime.UtcNow;
        Succeeded = succeeded;
        ResultSummary = Truncate(resultSummary, 8000);
        Error = Truncate(error, 8000);
        SetUpdateTime();
    }

    private static string Truncate(string value, int length) =>
        string.IsNullOrEmpty(value) || value.Length <= length ? value : value[..length];
}
