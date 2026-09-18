using Microsoft.Extensions.AI;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatFunctionToolFactoryTests
{
    [TestMethod]
    public async Task Create_BindsInstanceFunctionToItsPlugin()
    {
        var plugin = new RecordingPlugin();
        var method = typeof(RecordingPlugin).GetMethod(nameof(RecordingPlugin.Echo));

        var function = AdminChatFunctionToolFactory.Create(
            method,
            plugin,
            "Xncf_RecordingPlugin_Echo",
            "Returns the supplied value.");

        await function.InvokeAsync(new AIFunctionArguments
        {
            ["value"] = "AdminChat"
        });

        Assert.AreEqual("Xncf_RecordingPlugin_Echo", function.Name);
        Assert.AreEqual(1, plugin.InvocationCount);
        Assert.AreEqual("AdminChat", plugin.LastValue);
    }

    [TestMethod]
    public void Create_ExposesSelectionValuesAndDisplayTextInSchema()
    {
        var plugin = new RecordingPlugin();
        var method = typeof(RecordingPlugin).GetMethod(nameof(RecordingPlugin.Select));
        var function = AdminChatFunctionToolFactory.Create(
            method,
            plugin,
            "Xncf_RecordingPlugin_Select",
            "Selects a sandbox template.",
            new[]
            {
                new FunctionParameterInfo
                {
                    Name = nameof(SelectionRequest.TemplateKey),
                    ParameterType = ParameterType.DropDownList,
                    SelectionList = new SelectionList(
                        SelectionType.DropDownList,
                        new[]
                        {
                            new SelectionItem("python-exec", "Python Exec", "short Python workload"),
                            new SelectionItem("csharp-exec", "C# Exec", "short C# workload")
                        })
                }
            });

        using var schema = JsonDocument.Parse(function.JsonSchema.GetRawText());
        var parameterSchema = FindPropertySchema(schema.RootElement, nameof(SelectionRequest.TemplateKey));
        Assert.IsNotNull(parameterSchema);
        CollectionAssert.AreEqual(
            new[] { "python-exec", "csharp-exec" },
            parameterSchema.Value.GetProperty("enum").EnumerateArray().Select(item => item.GetString()).ToArray());
        StringAssert.Contains(parameterSchema.Value.GetProperty("description").GetString(), "Python Exec");
        StringAssert.Contains(parameterSchema.Value.GetProperty("description").GetString(), "python-exec");

        var result = function.InvokeAsync(new AIFunctionArguments
        {
            ["request"] = new SelectionRequest { TemplateKey = "csharp-exec" }
        }).GetAwaiter().GetResult();
        var resultText = result is JsonElement resultElement
            ? resultElement.GetString()
            : result?.ToString();
        Assert.AreEqual("csharp-exec", resultText);
    }

    [TestMethod]
    public async Task WorkflowTool_DeclaresInputAndOptionalWorkflowParameters()
    {
        var provider = new RecordingWorkflowProvider();
        var function = AdminChatWorkflowToolFactory.Create(
            provider,
            42,
            new WorkflowFunctionCallingDescriptor(
                7,
                "客户通知",
                "发送客户通知",
                new[]
                {
                    new WorkflowFunctionCallingParameter("customerId", "客户编号")
                }));

        using var schema = JsonDocument.Parse(function.JsonSchema.GetRawText());
        var properties = schema.RootElement.GetProperty("properties");

        Assert.IsTrue(properties.TryGetProperty("input", out _));
        Assert.IsTrue(properties.TryGetProperty("customerId", out _));
        CollectionAssert.Contains(
            schema.RootElement.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ToArray(),
            "input");

        var result = await function.InvokeAsync(new AIFunctionArguments
        {
            ["input"] = "请发送通知",
            ["customerId"] = "C-001"
        });

        Assert.AreEqual("workflow-output", result);
        Assert.AreEqual(42, provider.AdminUserId);
        Assert.AreEqual(7, provider.WorkflowId);
        Assert.AreEqual("请发送通知", provider.Input);
        Assert.AreEqual("C-001", provider.Parameters["customerId"]?.ToString());
    }

    private sealed class RecordingWorkflowProvider : IWorkflowFunctionCallingProvider
    {
        public int AdminUserId { get; private set; }
        public int WorkflowId { get; private set; }
        public string Input { get; private set; }
        public IReadOnlyDictionary<string, object?> Parameters { get; private set; }

        public Task<IReadOnlyList<WorkflowFunctionCallingDescriptor>> GetAvailableAsync(
            int adminUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkflowFunctionCallingDescriptor>>(
                Array.Empty<WorkflowFunctionCallingDescriptor>());

        public Task<WorkflowFunctionCallingResult> ExecuteAsync(
            int workflowId,
            int adminUserId,
            string input,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            WorkflowId = workflowId;
            AdminUserId = adminUserId;
            Input = input;
            Parameters = parameters;
            return Task.FromResult(new WorkflowFunctionCallingResult(true, "workflow-output", null));
        }
    }

    private sealed class RecordingPlugin
    {
        public int InvocationCount { get; private set; }
        public string LastValue { get; private set; }

        public string Echo(string value)
        {
            InvocationCount++;
            LastValue = value;
            return value;
        }

        public string Select(SelectionRequest request) => request.TemplateKey;
    }

    private sealed class SelectionRequest
    {
        public string TemplateKey { get; set; }
    }

    private static JsonElement? FindPropertySchema(JsonElement schema, string name)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (schema.TryGetProperty("properties", out var properties) &&
            properties.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in properties.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }

                var nested = FindPropertySchema(property.Value, name);
                if (nested.HasValue)
                {
                    return nested;
                }
            }
        }

        return null;
    }
}
