/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatFunctionToolFactory.cs
    文件功能描述：AdminChatFunctionToolFactory.cs 相关实现


    创建标识：Senparc - 20260820

    修改标识：Senparc - 20260822
    修改描述：v0.6.0 新增管理端 Chat 会话工作流能力

    修改标识：Senparc - 20260917
    修改描述：v0.10.0 绑定实例 FunctionRender 并暴露选择项 schema 元数据

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

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
        string description,
        IReadOnlyList<FunctionParameterInfo> parameterInfos = null)
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

        var function = AIFunctionFactory.Create(method, target, name, description);
        return parameterInfos == null || parameterInfos.Count == 0
            ? function
            : new FunctionRenderSchemaAIFunction(function, parameterInfos);
    }

    private sealed class FunctionRenderSchemaAIFunction : DelegatingAIFunction
    {
        private readonly JsonElement _jsonSchema;

        public FunctionRenderSchemaAIFunction(
            AIFunction innerFunction,
            IReadOnlyList<FunctionParameterInfo> parameterInfos)
            : base(innerFunction)
        {
            _jsonSchema = FunctionRenderAiSchemaBuilder.ApplySelectionMetadata(
                innerFunction.JsonSchema,
                parameterInfos);
        }

        public override JsonElement JsonSchema => _jsonSchema;
    }
}
