/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatFunctionToolFactory.cs
    文件功能描述：AdminChatFunctionToolFactory.cs 相关实现


    创建标识：Senparc - 20260820

    修改标识：Senparc - 20260822
    修改描述：v0.6.0 新增管理端 Chat 会话工作流能力

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using System;
using System.Reflection;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// Creates invocable AdminChat tools from FunctionRender methods.
/// </summary>
public static class AdminChatFunctionToolFactory
{
    public static AIFunction Create(
        MethodInfo method,
        object target,
        string name,
        string description)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!method.IsStatic)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (method.DeclaringType == null || !method.DeclaringType.IsInstanceOfType(target))
            {
                throw new ArgumentException(
                    $"Function target must be an instance of {method.DeclaringType?.FullName ?? "the declaring type"}.",
                    nameof(target));
            }
        }

        return AIFunctionFactory.Create(method, target, name, description);
    }
}
