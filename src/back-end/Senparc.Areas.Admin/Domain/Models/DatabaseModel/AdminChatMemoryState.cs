/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatMemoryState.cs
    文件功能描述：AdminChatMemoryState 相关定义与实现


    创建标识：Senparc - 20261103

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 完善聊天记忆的存储、检索与上下文处理

----------------------------------------------------------------*/

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(AdminChatMemoryState))]
[Index(nameof(TenantId), nameof(UserId), IsUnique = true)]
public class AdminChatMemoryState : EntityBase<int>
{
    public int UserId { get; set; }
    [Required, MaxLength(32), ConcurrencyCheck]
    public string Revision { get; set; } = "";
    [Required]
    public byte[] Payload { get; set; } = Array.Empty<byte>();
}
