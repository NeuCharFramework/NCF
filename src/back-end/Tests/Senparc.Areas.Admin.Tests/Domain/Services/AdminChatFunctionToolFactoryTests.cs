using Microsoft.Extensions.AI;
using Moq;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.Workspace.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    public async Task WorkspaceWrite_RejectsEmptyRelativePath()
    {
        var client = new Mock<IWorkspaceClient>();
        client.SetupGet(value => value.WorkspaceId).Returns("ws-test");
        var write = CreateWorkspaceTools(client.Object, allowWrite: true)
            .Single(tool => tool.Name == "workspace_write");

        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            async () => await write.InvokeAsync(new AIFunctionArguments
            {
                ["relativePath"] = string.Empty,
                ["content"] = "<html></html>"
            }));

        StringAssert.Contains(exception.Message, "relativePath 不能为空");
        client.Verify(
            value => value.WriteFileAsync(
                It.IsAny<WorkspaceWriteFileRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task WorkspaceWrite_ReturnsStructuredLineChangeSummary()
    {
        var client = new Mock<IWorkspaceClient>();
        client.SetupGet(value => value.WorkspaceId).Returns("ws-test");
        var metadata = new WorkspaceFileMetadata(
            "index.html",
            false,
            12,
            DateTime.UtcNow,
            DateTime.UtcNow,
            "\"etag\"");
        client.SetupSequence(value => value.GetFileInfoAsync(
                "index.html",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException())
            .ReturnsAsync(metadata);
        client.Setup(value => value.ReadFileAsync(
                "index.html",
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceFileContent(
                metadata,
                Encoding.UTF8.GetBytes("<p>one</p>\n<p>two</p>\n")));
        client.Setup(value => value.WriteFileAsync(
                It.IsAny<WorkspaceWriteFileRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceWriteFileResult(metadata, true));

        var write = CreateWorkspaceTools(client.Object, allowWrite: true)
            .Single(tool => tool.Name == "workspace_write");
        var result = await write.InvokeAsync(new AIFunctionArguments
        {
            ["relativePath"] = "index.html",
            ["content"] = "<p>one</p>\n<p>two</p>\n"
        });

        using var document = JsonDocument.Parse(result!.ToString()!);
        var change = document.RootElement.GetProperty("change");
        Assert.AreEqual("index.html", change.GetProperty("path").GetString());
        Assert.AreEqual("added", change.GetProperty("changeType").GetString());
        Assert.AreEqual(2, change.GetProperty("addedLines").GetInt32());
        Assert.AreEqual(0, change.GetProperty("removedLines").GetInt32());
    }

    [TestMethod]
    public async Task WorkspacePatch_ReturnsAddedAndRemovedLineCounts()
    {
        var client = new Mock<IWorkspaceClient>();
        client.SetupGet(value => value.WorkspaceId).Returns("ws-test");
        var beforeMetadata = new WorkspaceFileMetadata(
            "index.html",
            false,
            16,
            DateTime.UtcNow,
            DateTime.UtcNow,
            "\"before\"");
        var afterMetadata = beforeMetadata with
        {
            Length = 25,
            ETag = "\"after\""
        };
        client.SetupSequence(value => value.GetFileInfoAsync(
                "index.html",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(beforeMetadata)
            .ReturnsAsync(afterMetadata);
        client.SetupSequence(value => value.ReadFileAsync(
                "index.html",
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceFileContent(
                beforeMetadata,
                Encoding.UTF8.GetBytes("one\ntwo\n")))
            .ReturnsAsync(new WorkspaceFileContent(
                afterMetadata,
                Encoding.UTF8.GetBytes("one\nthree\nfour\n")));
        client.Setup(value => value.ApplyPatchAsync(
                It.IsAny<WorkspacePatchRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspacePatchResult(true, 1, "补丁应用成功。"));

        var patch = CreateWorkspaceTools(client.Object, allowWrite: true)
            .Single(tool => tool.Name == "workspace_patch");
        var result = await patch.InvokeAsync(new AIFunctionArguments
        {
            ["relativePath"] = "index.html",
            ["oldText"] = "two",
            ["newText"] = "three\nfour"
        });

        using var document = JsonDocument.Parse(result!.ToString()!);
        var change = document.RootElement.GetProperty("change");
        Assert.AreEqual(2, change.GetProperty("addedLines").GetInt32());
        Assert.AreEqual(1, change.GetProperty("removedLines").GetInt32());
    }

    [TestMethod]
    public async Task WorkspaceDelete_ReturnsRemovedLineCount()
    {
        var client = new Mock<IWorkspaceClient>();
        client.SetupGet(value => value.WorkspaceId).Returns("ws-test");
        var metadata = new WorkspaceFileMetadata(
            "obsolete.txt",
            false,
            10,
            DateTime.UtcNow,
            DateTime.UtcNow,
            "\"etag\"");
        client.Setup(value => value.GetFileInfoAsync(
                "obsolete.txt",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);
        client.Setup(value => value.ReadFileAsync(
                "obsolete.txt",
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceFileContent(
                metadata,
                Encoding.UTF8.GetBytes("first\nsecond\n")));

        var delete = CreateWorkspaceTools(client.Object, allowWrite: true)
            .Single(tool => tool.Name == "workspace_delete");
        client.Setup(value => value.DeleteEntryAsync(
                It.IsAny<WorkspaceDeleteEntryRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var result = await delete.InvokeAsync(new AIFunctionArguments
        {
            ["relativePath"] = "obsolete.txt"
        });

        using var document = JsonDocument.Parse(result!.ToString()!);
        var change = document.RootElement.GetProperty("change");
        Assert.AreEqual("deleted", change.GetProperty("changeType").GetString());
        Assert.AreEqual(0, change.GetProperty("addedLines").GetInt32());
        Assert.AreEqual(2, change.GetProperty("removedLines").GetInt32());
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
            Assert.IsTrue(schema.RootElement.GetProperty("properties").EnumerateObject()
                .Any(property => string.Equals(property.Name, nameof(SelectionRequest.TemplateKey), StringComparison.OrdinalIgnoreCase)));
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
    public void Create_BindsFlattenedRequestArguments()
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
                            new SelectionItem("python-exec", "Python Exec"),
                            new SelectionItem("jupyter-csharp", "JupyterLab C#")
                        })
                }
            });

        var result = function.InvokeAsync(new AIFunctionArguments
        {
            ["TemplateKey"] = "jupyter-csharp"
        }).GetAwaiter().GetResult();

        var resultText = result is JsonElement resultElement
            ? resultElement.GetString()
            : result?.ToString();
        Assert.AreEqual("jupyter-csharp", resultText);
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

    private static IReadOnlyList<AIFunction> CreateWorkspaceTools(
        IWorkspaceClient client,
        bool allowWrite)
    {
        var factory = typeof(AdminChatAiService).Assembly.GetType(
            "Senparc.Areas.Admin.Domain.Services.AdminChatWorkspaceToolFactory")!;
        return (IReadOnlyList<AIFunction>)factory.GetMethod("Create")!.Invoke(
            null,
            [client, allowWrite, false, null])!;
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
