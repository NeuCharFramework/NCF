/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatMemoryRequests.cs
    文件功能描述：AdminChatMemoryRequests 相关定义与实现


    创建标识：Senparc - 20261103

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 完善聊天记忆的存储、检索与上下文处理

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.OHS.Local.AppService;

public sealed class SaveAdminChatMemoryRequest
{
    public int SessionId { get; set; }
    [Required(AllowEmptyStrings = true), MaxLength(32)]
    public string Revision { get; set; }
    [Required(AllowEmptyStrings = true), MaxLength(32000)]
    public string Markdown { get; set; }
    [Required(AllowEmptyStrings = true), MaxLength(2000)]
    public string SystemMessage { get; set; }
}

public sealed class SetAdminChatMemoryRequest
{
    public int SessionId { get; set; }
    [Required(AllowEmptyStrings = true), MaxLength(32)]
    public string Revision { get; set; }
    public bool Enabled { get; set; }
    public AdminChatMemoryMode? Mode { get; set; }
}

public sealed class EditAdminChatMemoryRequest
{
    public int SessionId { get; set; }
    [Required(AllowEmptyStrings = true), MaxLength(32)]
    public string Revision { get; set; }
    public AdminChatMemoryEdit Entry { get; set; }
}

public sealed record AdminChatSystemMessageView(string Revision, string SystemMessage);
