using Microsoft.Extensions.AI;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// Creates an invocable AI tool for one selected Workflow.
/// </summary>
public static class AdminChatWorkflowToolFactory
{
    public static AIFunction Create(
        IWorkflowFunctionCallingProvider provider,
        int adminUserId,
        WorkflowFunctionCallingDescriptor workflow)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflow.Name);

        return new AdminChatWorkflowTool(provider, adminUserId, workflow);
    }

    private sealed class AdminChatWorkflowTool : AIFunction
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IWorkflowFunctionCallingProvider _provider;
        private readonly int _adminUserId;
        private readonly WorkflowFunctionCallingDescriptor _workflow;
        private readonly JsonElement _jsonSchema;

        public AdminChatWorkflowTool(
            IWorkflowFunctionCallingProvider provider,
            int adminUserId,
            WorkflowFunctionCallingDescriptor workflow)
        {
            _provider = provider;
            _adminUserId = adminUserId;
            _workflow = workflow;
            _jsonSchema = BuildJsonSchema(workflow);
        }

        public override string Name => $"Workflow_{_workflow.Id}";

        public override string Description =>
            string.IsNullOrWhiteSpace(_workflow.Description)
                ? $"执行工作流“{_workflow.Name}”。"
                : $"执行工作流“{_workflow.Name}”：{_workflow.Description}";

        public override JsonElement JsonSchema => _jsonSchema;

        public override JsonElement? ReturnJsonSchema =>
            JsonDocument.Parse("{\"type\":\"string\"}").RootElement.Clone();

        protected override async ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var input = ConvertToString(arguments.TryGetValue("input", out var inputValue) ? inputValue : null);
            if (string.IsNullOrWhiteSpace(input))
            {
                return "Workflow 调用失败：input 参数不能为空。";
            }

            var parameters = arguments
                .Where(pair => !string.Equals(pair.Key, "input", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            var result = await _provider.ExecuteAsync(
                _workflow.Id,
                _adminUserId,
                input,
                parameters,
                cancellationToken).ConfigureAwait(false);

            return result.Success
                ? result.Output ?? string.Empty
                : $"Workflow 调用失败：{result.ErrorMessage ?? "未知错误"}";
        }

        private static JsonElement BuildJsonSchema(WorkflowFunctionCallingDescriptor workflow)
        {
            var properties = new Dictionary<string, object?>
            {
                ["input"] = new
                {
                    type = "string",
                    description = "传给工作流的常规输入内容。"
                }
            };

            foreach (var parameter in workflow.Parameters ?? Array.Empty<WorkflowFunctionCallingParameter>())
            {
                if (string.IsNullOrWhiteSpace(parameter.Name) ||
                    string.Equals(parameter.Name, "input", StringComparison.OrdinalIgnoreCase) ||
                    properties.ContainsKey(parameter.Name))
                {
                    continue;
                }

                properties[parameter.Name] = new
                {
                    type = "string",
                    description = string.IsNullOrWhiteSpace(parameter.Description)
                        ? $"工作流参数“{parameter.Name}”（可选）。"
                        : $"{parameter.Description}（可选）"
                };
            }

            var schema = new
            {
                type = "object",
                properties,
                required = new[] { "input" },
                additionalProperties = false
            };
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(schema, SerializerOptions));
            return document.RootElement.Clone();
        }

        private static string ConvertToString(object value) =>
            value switch
            {
                null => string.Empty,
                string text => text,
                JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonElement element => element.GetRawText(),
                _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
            };
    }
}
