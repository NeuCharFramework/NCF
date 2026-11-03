/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatFunctionToolFactory.cs
    文件功能描述：AdminChatFunctionToolFactory.cs 相关实现


    创建标识：Senparc - 20260820

    修改标识：Senparc - 20260822
    修改描述：v0.6.0 新增管理端 Chat 会话工作流能力

    修改标识：Senparc - 20260917
    修改描述：v0.10.0 绑定实例 FunctionRender 并暴露选择项 schema 元数据

    修改标识：Senparc - 20261018
    修改描述：展平单请求 Function schema 并兼容扁平参数调用

    修改标识：Senparc - 20261021
    修改描述：v0.11.1 按当前模块构建函数工具

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 enhance Sandbox functionality with language validation and request handling improvements

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

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
            : new FunctionRenderSchemaAIFunction(function, method, parameterInfos);
    }

    private sealed class FunctionRenderSchemaAIFunction : DelegatingAIFunction
    {
        private readonly JsonElement _jsonSchema;
        private readonly string _requestParameterName;
        private readonly HashSet<string> _requestPropertyNames;

        public FunctionRenderSchemaAIFunction(
            AIFunction innerFunction,
            MethodInfo method,
            IReadOnlyList<FunctionParameterInfo> parameterInfos)
            : base(innerFunction)
        {
            var schema = FunctionRenderAiSchemaBuilder.ApplySelectionMetadata(
                innerFunction.JsonSchema,
                parameterInfos);
            _jsonSchema = FlattenRequestSchema(schema, method);
            var requestParameter = method.GetParameters().SingleOrDefault();
            _requestParameterName = requestParameter?.Name ?? "request";
            _requestPropertyNames = parameterInfos
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter?.Name))
                .Select(parameter => parameter.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public override JsonElement JsonSchema => _jsonSchema;

        protected override ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            if (arguments != null
                && !ContainsArgument(arguments, _requestParameterName)
                && arguments.Keys.Any(key => _requestPropertyNames.Contains(key)))
            {
                var requestProperties = arguments.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase);
                var normalizedArguments = new AIFunctionArguments(
                    new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        [_requestParameterName] = requestProperties
                    })
                {
                    Services = arguments.Services,
                    Context = arguments.Context
                };
                return base.InvokeCoreAsync(normalizedArguments, cancellationToken);
            }

            return base.InvokeCoreAsync(arguments, cancellationToken);
        }

        private static bool ContainsArgument(AIFunctionArguments arguments, string name) =>
            arguments.Keys.Any(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase));

        private static JsonElement FlattenRequestSchema(JsonElement schema, MethodInfo method)
        {
            var requestParameter = method.GetParameters().SingleOrDefault();
            if (requestParameter == null ||
                schema.ValueKind != JsonValueKind.Object ||
                JsonNode.Parse(schema.GetRawText()) is not JsonObject root ||
                !TryGetObjectProperty(root, "properties", out var properties) ||
                !TryGetObjectProperty(properties, requestParameter.Name, out var requestSchema) ||
                !TryGetObjectProperty(requestSchema, "properties", out _))
            {
                return schema.Clone();
            }

            using var document = JsonDocument.Parse(requestSchema.ToJsonString());
            return document.RootElement.Clone();
        }

        private static bool TryGetObjectProperty(
            JsonObject parent,
            string propertyName,
            out JsonObject value)
        {
            foreach (var property in parent)
            {
                if (string.Equals(property.Key, propertyName, StringComparison.OrdinalIgnoreCase) &&
                    property.Value is JsonObject objectValue)
                {
                    value = objectValue;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
