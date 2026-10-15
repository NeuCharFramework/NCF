/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawAdminIntegrationOptions.cs
    文件功能描述：WeixinClawAdminIntegrationOptions.cs implementation and project behavior.


    创建标识：Senparc - 20260926

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using System;

namespace Senparc.Areas.Admin.WeixinClawIntegration;

/// <summary>
/// Admin 与个人微信 Claw 的上层集成选项。
/// </summary>
public sealed class WeixinClawAdminIntegrationOptions
{
    public const string SectionName = "WeixinClawAdminIntegration";

    public bool Enabled { get; set; } = true;
    public string CommandPrefix { get; set; } = "/";
    public bool RequireCommandPrefix { get; set; } = true;
    public bool AllowPlainChat { get; set; } = true;
    public bool EnableNeuBell { get; set; } = true;
    public bool EnableWorkflow { get; set; } = true;
    public bool EnableHarness { get; set; }
    public int DefaultAiModelId { get; set; }
    public int MaxHarnessIterations { get; set; } = 16;
    public int MaxReplyLength { get; set; } = 1500;
    public int MaxInputLength { get; set; } = 8000;
    public TimeSpan HarnessTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
