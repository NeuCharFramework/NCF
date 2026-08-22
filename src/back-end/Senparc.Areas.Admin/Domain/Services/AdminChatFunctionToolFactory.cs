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
