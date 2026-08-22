/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowEngineTests.cs
    文件功能描述：NeuChar Workflow 声明式图安全校验测试
----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuCharWorkflowEngineTests
{
    [TestMethod]
    public void ParseAndValidateGraph_HumanInputNode_ShouldNormalizeExternalResumeSettings()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "human", "type": "human-input", "name": "补充信息", "config": { "externalResumeEnabled": true, "externalResumeKey": "resume-key" } }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "human" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);
        var node = graph.Nodes.Single(z => z.Id == "human");

        Assert.AreEqual("Workflow 等待人工输入", node.Config["title"]!.GetValue<string>());
        Assert.AreEqual("请补充必要信息：{{input}}", node.Config["prompt"]!.GetValue<string>());
        Assert.IsTrue(node.Config["externalResumeEnabled"]!.GetValue<bool>());
        Assert.AreEqual("resume-key", node.Config["externalResumeKey"]!.GetValue<string>());
    }

    [TestMethod]
    public async Task ValidateReferencesAsync_HumanInputNodeWithoutExternalKey_ShouldBeRejected()
    {
        var engine = CreateEngine();
        var graph = engine.ParseAndValidateGraph(
            """{ "nodes":[{ "id":"trigger", "type":"manual-trigger" },{ "id":"human", "type":"human-input", "config":{ "externalResumeEnabled":true } }], "edges":[{ "id":"edge-1", "source":"trigger", "target":"human" }] }""");

        var error = await engine.ValidateReferencesAsync(graph);

        StringAssert.Contains(error, "未设置恢复密钥");
    }

    [TestMethod]
    public async Task HumanInputService_ExternalResolution_ShouldRequireKeyAndCompleteRequest()
    {
        var service = new NeuCharWorkflowHumanInputService();
        var pending = service.Create(
            7,
            "workflow-7-run-42",
            13,
            "human-node",
            "人工补充",
            "请提供工单号",
            true,
            "resume-key");

        Assert.AreEqual(0, service.GetExternalPending(7, "incorrect-key").Count);
        Assert.AreEqual(pending.RequestId, service.GetExternalPending(7, "resume-key").Single().RequestId);

        var rejected = await service.ResolveFromExternalAsync(pending.RequestId, "incorrect-key", true, "T-100");
        Assert.IsFalse(rejected.Success);

        var resolved = await service.ResolveFromExternalAsync(pending.RequestId, "resume-key", true, "T-100");
        var decision = await pending.Completion;

        Assert.IsTrue(resolved.Success);
        Assert.IsTrue(decision.Approved);
        Assert.AreEqual("T-100", decision.Input);
        Assert.AreEqual(0, service.GetExternalPending(7, "resume-key").Count);
    }

    [TestMethod]
    public void ParseAndValidateGraph_ValidLinearGraph_ShouldNormalizeConfig()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger", "name": "手动触发" },
                { "id": "delay", "type": "delay", "name": "等待" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "delay" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual(2, graph.Nodes.Count);
        Assert.IsNotNull(graph.Nodes[0].Config);
        Assert.AreEqual("delay", graph.Edges[0].Target);
    }

    [TestMethod]
    public async Task ParseAndValidateGraph_LayoutDirection_ShouldPersistAndNormalize()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "layout": { "direction": "horizontal" },
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "end", "type": "end" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "end" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);
        var editableJson = await engine.BuildEditableGraphJsonAsync(graphJson);

        Assert.AreEqual("horizontal", graph.Layout.Direction);
        StringAssert.Contains(editableJson, "\"layout\"");
        StringAssert.Contains(editableJson, "\"horizontal\"");

        var legacyGraph = engine.ParseAndValidateGraph(
            """{ "nodes":[{ "id":"trigger", "type":"manual-trigger" }], "edges":[] }""");
        Assert.AreEqual("vertical", legacyGraph.Layout.Direction);
    }

    [TestMethod]
    public void ParseAndValidateGraph_Cycle_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "delay", "type": "delay" },
                { "id": "condition", "type": "condition" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "delay" },
                { "id": "edge-2", "source": "delay", "target": "condition" },
                { "id": "edge-3", "source": "condition", "target": "delay", "sourceHandle": "true" }
              ]
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "不允许工作流形成循环");
    }

    [TestMethod]
    public void ParseAndValidateGraph_MultipleTriggers_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "manual", "type": "manual-trigger" },
                { "id": "interval", "type": "interval-trigger" }
              ],
              "edges": []
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "只能包含一个触发器");
    }

    [TestMethod]
    public void CalculateNextRun_IntervalBelowMinimum_ShouldClampToOneMinute()
    {
        var now = new DateTime(2026, 8, 8, 0, 0, 0, DateTimeKind.Utc);

        var result = NeuCharWorkflowEngine.CalculateNextRun(
            "interval",
            "{\"intervalSeconds\":1}",
            now);

        Assert.AreEqual(now.AddMinutes(1), result);
    }

    [TestMethod]
    public void ParseAndValidateGraph_DisconnectedNode_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "orphan", "type": "delay", "name": "孤立节点" }
              ],
              "edges": []
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "未连接到触发器");
    }

    [TestMethod]
    public async Task ParseAndValidateGraph_DraftWithDisconnectedNode_ShouldRemainEditable()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "orphan", "type": "delay", "name": "草稿等待" }
              ],
              "edges": []
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson, requireAllNodesReachable: false);

        Assert.AreEqual(1, engine.GetDisconnectedNodes(graph).Count);
        var editableJson = await engine.BuildEditableGraphJsonAsync(graphJson);
        StringAssert.Contains(editableJson, "orphan");
    }

    [TestMethod]
    public void ParseAndValidateGraph_LegacyConditionEdge_ShouldNormalizeToTrueBranch()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "condition", "type": "condition" },
                { "id": "end", "type": "end" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "condition" },
                { "id": "edge-2", "source": "condition", "target": "end" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual("default", graph.Edges[0].SourceHandle);
        Assert.AreEqual("true", graph.Edges[1].SourceHandle);
    }

    [TestMethod]
    public void ParseAndValidateGraph_OrdinaryNodeWithTwoOutputs_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "end-a", "type": "end" },
                { "id": "end-b", "type": "end" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "end-a" },
                { "id": "edge-2", "source": "trigger", "target": "end-b" }
              ]
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "只能连接一个后续节点");
    }

    [TestMethod]
    public void ParseAndValidateGraph_ParallelWithMultipleOutputs_ShouldBeAllowed()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "parallel", "type": "parallel", "name": "并行" },
                { "id": "end-a", "type": "end", "name": "分支 A" },
                { "id": "end-b", "type": "end", "name": "分支 B" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "parallel" },
                { "id": "edge-2", "source": "parallel", "target": "end-a" },
                { "id": "edge-3", "source": "parallel", "target": "end-b" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual(2, graph.Edges.Count(edge => edge.Source == "parallel"));
        Assert.IsTrue(graph.Nodes.Any(node => node.Type == "parallel"));
    }

    [TestMethod]
    public void ParseAndValidateGraph_OrdinaryNodeWithTwoInputs_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "condition", "type": "condition" },
                { "id": "delay", "type": "delay", "name": "普通节点" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "condition" },
                { "id": "edge-2", "source": "condition", "target": "delay", "sourceHandle": "true" },
                { "id": "edge-3", "source": "condition", "target": "delay", "sourceHandle": "false" }
              ]
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "只允许一个上游连接");
    }

    [TestMethod]
    public void ParseAndValidateGraph_AggregateWithTwoInputs_ShouldBeAllowed()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "condition", "type": "condition" },
                { "id": "aggregate", "type": "aggregate", "name": "聚合" },
                { "id": "console", "type": "console", "name": "Console 打印" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "condition" },
                { "id": "edge-2", "source": "condition", "target": "aggregate", "sourceHandle": "true" },
                { "id": "edge-3", "source": "condition", "target": "aggregate", "sourceHandle": "false" },
                { "id": "edge-4", "source": "aggregate", "target": "console" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual(2, graph.Edges.Count(z => z.Target == "aggregate"));
        Assert.IsTrue(graph.Nodes.Any(z => z.Type == "console"));
    }

    [TestMethod]
    public void ParseAndValidateGraph_LegacyAggregate_ShouldAddRawArrayOutputTemplate()
    {
        var engine = CreateEngine();
        var graph = engine.ParseAndValidateGraph(
            """{ "nodes":[{ "id":"trigger", "type":"manual-trigger" },{ "id":"aggregate", "type":"aggregate" }], "edges":[{ "id":"edge-1", "source":"trigger", "target":"aggregate" }] }""");

        Assert.AreEqual("{{input}}", graph.Nodes.Single(node => node.Id == "aggregate").Config["outputTemplate"]!.GetValue<string>());
    }

    [TestMethod]
    public void ParseAndValidateGraph_LegacyConsole_ShouldAddRawInputPrintTemplate()
    {
        var engine = CreateEngine();
        var graph = engine.ParseAndValidateGraph(
            """{ "nodes":[{ "id":"trigger", "type":"manual-trigger" },{ "id":"console", "type":"console" }], "edges":[{ "id":"edge-1", "source":"trigger", "target":"console" }] }""");

        Assert.AreEqual("{{input}}", graph.Nodes.Single(node => node.Id == "console").Config["printTemplate"]!.GetValue<string>());
    }

    [TestMethod]
    public void ParseAndValidateGraph_LegacyLoop_ShouldAddBoundedDefaultCount()
    {
        var engine = CreateEngine();
        var graph = engine.ParseAndValidateGraph(
            """{ "nodes":[{ "id":"trigger", "type":"manual-trigger" },{ "id":"loop", "type":"loop" },{ "id":"console", "type":"console" }], "edges":[{ "id":"edge-1", "source":"trigger", "target":"loop" },{ "id":"edge-2", "source":"loop", "target":"console" }] }""");

        Assert.AreEqual(3, graph.Nodes.Single(node => node.Id == "loop").Config["count"]!.GetValue<int>());
    }

    [TestMethod]
    public void ParseAndValidateGraph_ExplicitLoopBoundary_ShouldAllowContinuation()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "loop", "type": "loop", "config": { "count": 2 } },
                { "id": "body", "type": "delay" },
                { "id": "loop-end", "type": "loop-end" },
                { "id": "after", "type": "console" },
                { "id": "end", "type": "end" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "loop" },
                { "id": "edge-2", "source": "loop", "target": "body" },
                { "id": "edge-3", "source": "body", "target": "loop-end" },
                { "id": "edge-4", "source": "loop-end", "target": "after" },
                { "id": "edge-5", "source": "after", "target": "end" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual("loop-end", graph.Nodes.Single(node => node.Id == "loop-end").Type);
        Assert.AreEqual("after", graph.Edges.Single(edge => edge.Source == "loop-end").Target);
    }

    [TestMethod]
    public void ParseAndValidateGraph_ConditionBreak_ShouldUseLoopEndBreakInput()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "loop", "type": "loop", "config": { "count": 5 } },
                { "id": "condition", "type": "condition", "config": { "breakOn": "true" } },
                { "id": "body", "type": "delay" },
                { "id": "loop-end", "type": "loop-end", "config": { "loopId": "loop" } },
                { "id": "after", "type": "console" },
                { "id": "end", "type": "end" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "loop" },
                { "id": "edge-2", "source": "loop", "target": "condition" },
                { "id": "edge-3", "source": "condition", "target": "body", "sourceHandle": "false" },
                { "id": "edge-4", "source": "condition", "target": "loop-end", "sourceHandle": "break", "targetHandle": "break" },
                { "id": "edge-5", "source": "body", "target": "loop-end", "targetHandle": "continue" },
                { "id": "edge-6", "source": "loop-end", "target": "after" },
                { "id": "edge-7", "source": "after", "target": "end" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual("break", graph.Edges.Single(edge => edge.Id == "edge-4").SourceHandle);
        Assert.AreEqual("break", graph.Edges.Single(edge => edge.Id == "edge-4").TargetHandle);
        Assert.AreEqual("continue", graph.Edges.Single(edge => edge.Id == "edge-5").TargetHandle);
    }

    [TestMethod]
    public void ParseAndValidateGraph_NestedLoops_ShouldValidateEachExplicitBoundary()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "outer-loop", "type": "loop", "config": { "count": 2 } },
                { "id": "inner-loop", "type": "loop", "config": { "count": 3 } },
                { "id": "inner-body", "type": "delay" },
                { "id": "inner-end", "type": "loop-end", "config": { "loopId": "inner-loop" } },
                { "id": "outer-body", "type": "console" },
                { "id": "outer-end", "type": "loop-end", "config": { "loopId": "outer-loop" } },
                { "id": "after", "type": "console" },
                { "id": "end", "type": "end" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "outer-loop" },
                { "id": "edge-2", "source": "outer-loop", "target": "inner-loop" },
                { "id": "edge-3", "source": "inner-loop", "target": "inner-body" },
                { "id": "edge-4", "source": "inner-body", "target": "inner-end", "targetHandle": "continue" },
                { "id": "edge-5", "source": "inner-end", "target": "outer-body" },
                { "id": "edge-6", "source": "outer-body", "target": "outer-end", "targetHandle": "continue" },
                { "id": "edge-7", "source": "outer-end", "target": "after" },
                { "id": "edge-8", "source": "after", "target": "end" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual("inner-loop", graph.Nodes.Single(node => node.Id == "inner-end").Config["loopId"]!.GetValue<string>());
        Assert.AreEqual("outer-loop", graph.Nodes.Single(node => node.Id == "outer-end").Config["loopId"]!.GetValue<string>());
    }

    [TestMethod]
    public void ParseAndValidateGraph_LoopBoundaryWithBranch_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "loop", "type": "loop", "config": { "count": 2 } },
                { "id": "parallel", "type": "parallel" },
                { "id": "body-a", "type": "delay" },
                { "id": "body-b", "type": "delay" },
                { "id": "loop-end", "type": "loop-end" },
                { "id": "end", "type": "end" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "loop" },
                { "id": "edge-2", "source": "loop", "target": "parallel" },
                { "id": "edge-3", "source": "parallel", "target": "body-a" },
                { "id": "edge-4", "source": "parallel", "target": "body-b" },
                { "id": "edge-5", "source": "body-a", "target": "loop-end" },
                { "id": "edge-6", "source": "body-b", "target": "loop-end" },
                { "id": "edge-7", "source": "loop-end", "target": "end" }
              ]
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "普通节点组成的单一路径");
    }

    [TestMethod]
    public async Task ValidateReferencesAsync_Loop_ShouldRequireBoundedIntegerCount()
    {
        var engine = CreateEngine();
        var graph = engine.ParseAndValidateGraph(
            """{ "nodes":[{ "id":"trigger", "type":"manual-trigger" },{ "id":"loop", "type":"loop", "name":"循环", "config":{ "count":100001 } }], "edges":[{ "id":"edge-1", "source":"trigger", "target":"loop" }] }""");

        var error = await engine.ValidateReferencesAsync(graph);

        StringAssert.Contains(error, "1 到 100000");
    }

    [TestMethod]
    public void LoopCount_ShouldResolveSingleNumericRuntimeValue()
    {
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "TryResolveLoopCount",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] arguments =
        {
            new JsonObject { ["count"] = "4" },
            JsonValue.Create("input"),
            new Dictionary<string, JsonNode>(),
            new Dictionary<string, JsonNode>(),
            0,
            null
        };

        var success = (bool)method.Invoke(null, arguments)!;

        Assert.IsTrue(success);
        Assert.AreEqual(4, (int)arguments[4]!);
        Assert.IsNull(arguments[5]);
    }

    [TestMethod]
    public void LoopCount_ShouldResolveUpstreamRuntimeValue()
    {
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "TryResolveLoopCount",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] arguments =
        {
            new JsonObject
            {
                ["count"] = new JsonObject
                {
                    ["$source"] = new JsonObject { ["nodeId"] = "source", ["path"] = "$" }
                }
            },
            JsonValue.Create("input"),
            new Dictionary<string, JsonNode> { ["source"] = JsonValue.Create("4")! },
            new Dictionary<string, JsonNode>(),
            0,
            null
        };

        var success = (bool)method.Invoke(null, arguments)!;

        Assert.IsTrue(success);
        Assert.AreEqual(4, (int)arguments[4]!);
    }

    [TestMethod]
    public async Task LoopCount_ShouldResolveFormulaRuntimeValue()
    {
        var engine = CreateEngine();
        var graph = engine.ParseAndValidateGraph(
            """{ "nodes":[{ "id":"trigger", "type":"manual-trigger" },{ "id":"loop", "type":"loop", "name":"循环", "config":{ "count":{ "$template":{ "text":"{{= toInt(input) }}", "bindings":[] } } } }], "edges":[{ "id":"edge-1", "source":"trigger", "target":"loop" }] }""");

        Assert.IsNull(await engine.ValidateReferencesAsync(graph));

        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "TryResolveLoopCount",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] arguments =
        {
            graph.Nodes.Single(node => node.Id == "loop").Config,
            JsonValue.Create("4"),
            new Dictionary<string, JsonNode>(),
            new Dictionary<string, JsonNode>(),
            0,
            null
        };

        var success = (bool)method.Invoke(null, arguments)!;

        Assert.IsTrue(success);
        Assert.AreEqual(4, (int)arguments[4]!);
    }

    [TestMethod]
    public async Task LoopCount_ShouldResolveFormulaUsingWorkflowVariables()
    {
        var engine = CreateEngine();
        var graph = engine.ParseAndValidateGraph(
            """{ "nodes":[{ "id":"trigger", "type":"manual-trigger" },{ "id":"loop", "type":"loop", "name":"循环", "config":{ "count":{ "$template":{ "text":"{{= toInt( toNumber(vars.end) - toNumber(vars.number)) }}", "bindings":[] } } } }], "edges":[{ "id":"edge-1", "source":"trigger", "target":"loop" }] }""");

        Assert.IsNull(await engine.ValidateReferencesAsync(graph));

        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "TryResolveLoopCount",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] arguments =
        {
            graph.Nodes.Single(node => node.Id == "loop").Config,
            JsonValue.Create("input"),
            new Dictionary<string, JsonNode>
            {
                ["__workflow_variables__"] = new JsonObject
                {
                    ["end"] = 9,
                    ["number"] = 4
                }
            },
            new Dictionary<string, JsonNode>(),
            0,
            null
        };

        var success = (bool)method.Invoke(null, arguments)!;

        Assert.IsTrue(success);
        Assert.AreEqual(5, (int)arguments[4]!);
    }

    [TestMethod]
    public void ParseAndValidateGraph_MergeWithTwoInputs_ShouldBeAllowed()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "condition", "type": "condition" },
                { "id": "merge", "type": "merge", "name": "逐项合流" },
                { "id": "console", "type": "console" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "condition" },
                { "id": "edge-2", "source": "condition", "target": "merge", "sourceHandle": "true" },
                { "id": "edge-3", "source": "condition", "target": "merge", "sourceHandle": "false" },
                { "id": "edge-4", "source": "merge", "target": "console" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual(2, graph.Edges.Count(edge => edge.Target == "merge"));
        Assert.IsTrue(graph.Nodes.Any(node => node.Type == "merge"));
    }

    [TestMethod]
    public void ParseAndValidateGraph_AggregateAfterMerge_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "merge", "type": "merge" },
                { "id": "aggregate", "type": "aggregate" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "merge" },
                { "id": "edge-2", "source": "merge", "target": "aggregate" }
              ]
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(() => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "不能位于逐项合流节点之后");
    }

    [TestMethod]
    public void ParseAndValidateGraph_AggregateAfterLoop_ShouldBeRejected()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "loop", "type": "loop", "config": { "count": 2 } },
                { "id": "aggregate", "type": "aggregate" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "loop" },
                { "id": "edge-2", "source": "loop", "target": "aggregate" }
              ]
            }
            """;

        var exception = Assert.ThrowsException<InvalidOperationException>(() => engine.ParseAndValidateGraph(graphJson));

        StringAssert.Contains(exception.Message, "不能位于循环节点之后");
    }

    [TestMethod]
    public async Task ValidateReferencesAsync_Aggregate_ShouldRequireConfiguredOutputContent()
    {
        var engine = CreateEngine();
        var graph = engine.ParseAndValidateGraph(
            """{ "nodes":[{ "id":"trigger", "type":"manual-trigger" },{ "id":"aggregate", "type":"aggregate", "name":"汇总", "config":{ "outputTemplate":"" } }], "edges":[{ "id":"edge-1", "source":"trigger", "target":"aggregate" }] }""");

        var error = await engine.ValidateReferencesAsync(graph);

        StringAssert.Contains(error, "必须设置输出内容");
    }

    [TestMethod]
    public void AggregateOutputTemplate_ShouldPreserveArrayOrRenderRestrictedExpression()
    {
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "ResolveAggregateOutput",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var input = new JsonArray(JsonValue.Create("A"), JsonValue.Create("B"));
        var raw = (JsonNode)method.Invoke(null, new object[]
        {
            new JsonObject { ["outputTemplate"] = "{{input}}" },
            input,
            new Dictionary<string, JsonNode>(),
            new Dictionary<string, JsonNode>()
        })!;
        var rendered = (JsonNode)method.Invoke(null, new object[]
        {
            new JsonObject { ["outputTemplate"] = "共 {{= length(input) }} 项" },
            input,
            new Dictionary<string, JsonNode>(),
            new Dictionary<string, JsonNode>()
        })!;

        Assert.IsInstanceOfType(raw, typeof(JsonArray));
        Assert.AreEqual(2, raw.AsArray().Count);
        Assert.AreEqual("共 2 项", rendered.GetValue<string>());
    }

    [TestMethod]
    public void AggregateOutputTemplate_ShouldResolveRichTextTemplateWrapper()
    {
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "ResolveAggregateOutput",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var template = new JsonObject
        {
            ["outputTemplate"] = new JsonObject
            {
                ["$template"] = new JsonObject
                {
                    ["text"] = "汇总 {{= length(input) }} 项"
                }
            }
        };
        var input = new JsonArray(JsonValue.Create("A"), JsonValue.Create("B"));

        var rendered = (JsonNode)method.Invoke(null, new object[]
        {
            template,
            input,
            new Dictionary<string, JsonNode>(),
            new Dictionary<string, JsonNode>()
        })!;

        Assert.AreEqual("汇总 2 项", rendered.GetValue<string>());
    }

    [TestMethod]
    public void ConsolePrintTemplate_ShouldRenderWithoutChangingRawInput()
    {
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "ResolveConsolePrintOutput",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var input = JsonValue.Create("vip");
        var raw = (JsonNode)method.Invoke(null, new object[]
        {
            new JsonObject { ["printTemplate"] = "{{input}}" },
            input,
            new Dictionary<string, JsonNode>(),
            new Dictionary<string, JsonNode>()
        })!;
        var rendered = (JsonNode)method.Invoke(null, new object[]
        {
            new JsonObject { ["printTemplate"] = "收到：{{= upper(input) }}" },
            input,
            new Dictionary<string, JsonNode>(),
            new Dictionary<string, JsonNode>()
        })!;

        Assert.AreEqual("vip", raw.GetValue<string>());
        Assert.AreEqual("收到：VIP", rendered.GetValue<string>());
    }

    [TestMethod]
    public void RuntimeText_ShouldResolveFormulaWhenEnteredWithoutTemplateWrapper()
    {
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "ResolveRuntimeValue",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var resolved = (JsonNode)method.Invoke(null, new object[]
        {
            JsonValue.Create("标题：{{= upper(input) }}"),
            JsonValue.Create("vip"),
            new Dictionary<string, JsonNode>(),
            new Dictionary<string, JsonNode>()
        })!;

        Assert.AreEqual("标题：VIP", resolved.GetValue<string>());
    }

    [TestMethod]
    public async Task ValidateReferencesAsync_NeuBell_ShouldRejectInvalidRichTextFormula()
    {
        var engine = CreateEngine();
        const string graphJson = """{ "nodes":[{ "id":"trigger", "type":"manual-trigger" },{ "id":"notify", "type":"neubell", "name":"发送纽铃", "config":{ "title":"{{= unknown(input) }}", "summary":"内容" } }], "edges":[{ "id":"edge-1", "source":"trigger", "target":"notify" }] }""";
        var graph = engine.ParseAndValidateGraph(graphJson);

        var error = await engine.ValidateReferencesAsync(graph);

        StringAssert.Contains(error, "文本表达式无效");
    }

    [TestMethod]
    public void AggregateInput_ShouldContainOnlyActiveIncomingEdgesInGraphOrder()
    {
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "BuildAggregateInput",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var aggregate = new NeuCharWorkflowNode { Id = "aggregate", Type = "aggregate" };
        var graph = new NeuCharWorkflowGraph
        {
            Nodes = { aggregate },
            Edges =
            {
                new NeuCharWorkflowEdge { Id = "true-edge", Source = "condition", Target = "aggregate", SourceHandle = "true" },
                new NeuCharWorkflowEdge { Id = "false-edge", Source = "condition", Target = "aggregate", SourceHandle = "false" }
            }
        };
        var outputs = new Dictionary<string, JsonNode>
        {
            ["condition"] = JsonValue.Create("selected")!
        };

        var value = (JsonArray)method.Invoke(null, new object[]
        {
            graph,
            aggregate,
            new HashSet<string> { "true-edge" },
            outputs
        })!;

        Assert.AreEqual(1, value.Count);
        Assert.AreEqual("selected", value[0]!.GetValue<string>());
    }

    [TestMethod]
    public void ParseAndValidateGraph_FunctionWithTwoInputs_ShouldBeAllowed()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "condition", "type": "condition" },
                { "id": "function", "type": "function", "name": "共享 Function" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "condition" },
                { "id": "edge-2", "source": "condition", "target": "function", "sourceHandle": "true" },
                { "id": "edge-3", "source": "condition", "target": "function", "sourceHandle": "false" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);

        Assert.AreEqual(2, graph.Edges.Count(z => z.Target == "function"));
    }

    [TestMethod]
    public async Task FunctionScope_ConcurrentOperations_ShouldResolveDifferentScopedFunctionServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<NeuCharWorkflowFunctionService>(_ =>
            new NeuCharWorkflowFunctionService(null!, null!));
        using var serviceProvider = services.BuildServiceProvider();
        var engine = new NeuCharWorkflowEngine(
            null!,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            null!,
            null!,
            Array.Empty<IWorkflowObjectProvider>());
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "ExecuteInFunctionScopeAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(int));
        Func<NeuCharWorkflowFunctionService, Task<int>> operation = service =>
            Task.FromResult(RuntimeHelpers.GetHashCode(service));

        var tasks = Enumerable.Range(0, 2)
            .Select(_ => (Task<int>)method.Invoke(engine, new object[] { operation })!)
            .ToArray();
        var identities = await Task.WhenAll(tasks);

        Assert.AreNotEqual(identities[0], identities[1],
            "Concurrent Function operations must not share the same scoped Function service/DbContext graph.");
    }

    [TestMethod]
    public async Task AgentGroupExecution_ShouldPassHilPolicyThroughProviderParameters()
    {
        var provider = new CapturingWorkflowObjectProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowObjectProvider>(provider);
        using var serviceProvider = services.BuildServiceProvider();
        var engine = new NeuCharWorkflowEngine(
            null!,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            null!,
            null!,
            new[] { provider });
        var workflow = new Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow(
            "HIL 参数测试",
            37);
        var node = new NeuCharWorkflowNode
        {
            Id = "group-node",
            Type = "agent-group",
            Name = "审批组",
            Config = new JsonObject
            {
                ["providerId"] = provider.ProviderId,
                ["objectId"] = "group:42",
                ["prompt"] = "{{input}}",
                [WorkflowObjectExecutionParameters.HumanInTheLoopLevel] = 3,
                [WorkflowObjectExecutionParameters.PluginToolPermission] = 2,
                [WorkflowObjectExecutionParameters.McpToolPermission] = 3,
                [WorkflowObjectExecutionParameters.IncludeHumanParticipant] = true,
                [WorkflowObjectExecutionParameters.ChatMaxRound] = 2
            }
        };
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "ExecuteWorkflowObjectNodeAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var invocation = (Task)method.Invoke(engine, new object[]
        {
            workflow,
            node,
            JsonValue.Create("继续处理")!,
            new Dictionary<string, JsonNode>(),
            new Dictionary<string, JsonNode>(),
            "workflow-17-run-4f33d29e185c4f43b67c890af104674e",
            CancellationToken.None
        })!;

        await invocation;

        Assert.IsNotNull(provider.LastRequest);
        Assert.AreEqual("3", provider.LastRequest.Parameters![WorkflowObjectExecutionParameters.HumanInTheLoopLevel]);
        Assert.AreEqual("2", provider.LastRequest.Parameters[WorkflowObjectExecutionParameters.PluginToolPermission]);
        Assert.AreEqual("3", provider.LastRequest.Parameters[WorkflowObjectExecutionParameters.McpToolPermission]);
        Assert.AreEqual("True", provider.LastRequest.Parameters[WorkflowObjectExecutionParameters.IncludeHumanParticipant]);
        Assert.AreEqual("2", provider.LastRequest.Parameters[WorkflowObjectExecutionParameters.ChatMaxRound]);
        Assert.AreEqual(37, provider.LastRequest.AdminUserId);
    }

    [TestMethod]
    public void ExecutionLog_ReplaySnapshotAndEvents_ShouldBeStoredSeparately()
    {
        var executionLog = new Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflowExecutionLog(
            12,
            "回看测试",
            "workflow-12-run-0123456789abcdef0123456789abcdef");

        executionLog.SetReplaySnapshot("a".PadLeft(64, 'a'), "{\"graphJson\":\"{}\"}");
        executionLog.Complete(true, "完成", null, "[{\"nodeId\":\"trigger\"}]");

        Assert.AreEqual(64, executionLog.ReplaySnapshotHash!.Length);
        Assert.AreEqual("{\"graphJson\":\"{}\"}", executionLog.ReplaySnapshotJson);
        Assert.AreEqual("[{\"nodeId\":\"trigger\"}]", executionLog.ReplayEventsJson);
        Assert.IsTrue(executionLog.Succeeded == true && executionLog.FinishedAt != null);
    }

    [TestMethod]
    public void ReplayProgress_ShouldRetainNodeInputAlongsideOutput()
    {
        var progress = new NeuCharWorkflowProgress(
            "function-1",
            "查询",
            "success",
            "节点执行完成。",
            "{\"result\":\"ok\"}",
            DateTimeOffset.UtcNow,
            null,
            "{\"keyword\":\"workflow\"}");

        Assert.AreEqual("{\"keyword\":\"workflow\"}", progress.Input);
        Assert.AreEqual("{\"result\":\"ok\"}", progress.Output);
    }

    [TestMethod]
    public async Task ValidateReferencesAsync_NeuBellNode_ShouldAcceptSupportedConsumptionModes()
    {
        var engine = CreateEngine();
        const string graphJson =
            """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "notify", "type": "neubell", "name": "发送纽铃", "config": { "title": "任务完成", "summary": "请查看 {{input}}", "consumeMode": "item" } }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "notify" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);
        Assert.IsNull(await engine.ValidateReferencesAsync(graph));

        graph.Nodes.Single(node => node.Id == "notify").Config["consumeMode"] = "unsupported";
        var error = await engine.ValidateReferencesAsync(graph);
        StringAssert.Contains(error, "消费方式无效");
    }

    [TestMethod]
    public void BuildOutputDescriptor_AppResponseList_ShouldExposeElementFieldsAndArrayShape()
    {
        var method = typeof(NeuCharWorkflowEngineTests).GetMethod(
            nameof(ListOutputFunction),
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var output = NeuCharWorkflowFunctionService.BuildOutputDescriptor(method);

        Assert.IsTrue(output.IsArray);
        Assert.AreEqual("object", output.TypeName);
        var name = output.Fields.Single(z => z.Path == "$.Name");
        Assert.AreEqual("string", name.TypeName);
        Assert.IsTrue(name.RequiresIndex);
        var tags = output.Fields.Single(z => z.Path == "$.Tags");
        Assert.IsTrue(tags.IsArray);
        Assert.IsTrue(tags.RequiresIndex);
    }

    [TestMethod]
    public void WorkflowFunctionSchemaBuilder_UnnamedParameter_ShouldUseStableDraftKey()
    {
        var descriptor = new NeuCharFunctionDescriptor(
            "module",
            "测试模块",
            "1.0.0",
            true,
            "test-function",
            "测试 Function",
            null,
            new[] { new Senparc.Ncf.XncfBase.FunctionParameterInfo { Name = null, Title = null } });

        var parameter = WorkflowFunctionSchemaBuilder.Build(descriptor).Single();

        Assert.AreEqual("parameter_1", parameter.Name);
        Assert.AreEqual("Function 参数元数据缺少字段名；当前仅可保存草稿，修复或更新模块后才能运行。", parameter.MetadataError);
        Assert.IsTrue(parameter.HasSyntheticName);
    }

    [TestMethod]
    public void WorkflowFunctionSchemaBuilder_SelectionParameter_ShouldRetainSandboxStyleMetadata()
    {
        var descriptor = new NeuCharFunctionDescriptor(
            "sandbox",
            "Sandbox",
            "1.0.0",
            true,
            "create",
            "创建沙箱",
            null,
            new[]
            {
                new Senparc.Ncf.XncfBase.FunctionParameterInfo
                {
                    Name = "TemplateKey",
                    Title = "模板",
                    Description = "选择沙箱模板",
                    ParameterType = Senparc.Ncf.XncfBase.ParameterType.DropDownList,
                    SystemType = "String",
                    SelectionList = new Senparc.Ncf.XncfBase.Functions.SelectionList(
                        Senparc.Ncf.XncfBase.Functions.SelectionType.DropDownList,
                        new[] { new Senparc.Ncf.XncfBase.Functions.SelectionItem("python", "Python Exec", "Python 模板", true) })
                }
            });

        var parameter = WorkflowFunctionSchemaBuilder.Build(descriptor).Single();

        Assert.AreEqual("TemplateKey", parameter.Name);
        Assert.AreEqual("模板", parameter.Title);
        Assert.AreEqual("选择沙箱模板", parameter.Description);
        Assert.AreEqual(1, parameter.ParameterType);
        Assert.AreEqual("Python Exec", parameter.Options.Single().Text);
    }

    [TestMethod]
    public void ValidateRequiredParameters_UnnamedMetadata_ShouldRejectExecution()
    {
        var error = NeuCharWorkflowFunctionService.ValidateRequiredParameters(
            new[] { new Senparc.Ncf.XncfBase.FunctionParameterInfo { Name = null, Title = "未知参数" } },
            "{}");

        StringAssert.Contains(error, "缺少字段名");
    }

    [TestMethod]
    public void ResolveBinding_FunctionSelection_ShouldUseResolvedSelectionValue()
    {
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "ResolveBinding",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var binding = JsonNode.Parse(
            """
            {
              "nodeId": "source",
              "path": "$.__functionInput.crawlMode",
              "sourceKind": "function-selection",
              "sourceParameterName": "crawlMode"
            }
            """)!.AsObject();
        var selectionInputs = new Dictionary<string, JsonNode>
        {
            ["source"] = JsonNode.Parse("""{ "crawlMode": "full" }""")!
        };

        var value = (JsonNode?)method.Invoke(null, new object[]
        {
            binding,
            new Dictionary<string, JsonNode>(),
            selectionInputs
        });

        Assert.AreEqual("full", value!.GetValue<string>());
    }

    [TestMethod]
    public void ResolveRuntimeValue_Template_ShouldInterpolateMultipleBindings()
    {
        var method = typeof(NeuCharWorkflowEngine).GetMethod(
            "ResolveRuntimeValue",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var template = JsonNode.Parse(
            """
            {
              "$template": {
                "text": "模板={{template}}；运行时={{runtime}}；表达式={{=upper(template)}}；输入={{input}}",
                "bindings": [
                  { "token": "template", "source": { "nodeId": "source", "path": "$.template" } },
                  { "token": "runtime", "source": { "nodeId": "source", "path": "$.runtime" } }
                ]
              }
            }
            """)!;
        var outputs = new Dictionary<string, JsonNode>
        {
            ["source"] = JsonNode.Parse("""{ "template": "Python Exec", "runtime": "Docker" }""")!
        };

        var value = (JsonNode?)method.Invoke(null, new object[]
        {
            template,
            JsonValue.Create("来自触发器"),
            outputs,
            new Dictionary<string, JsonNode>()
        });

        Assert.AreEqual("模板=Python Exec；运行时=Docker；表达式=PYTHON EXEC；输入=来自触发器", value!.GetValue<string>());
    }

    [TestMethod]
    public void TemplateExpression_ShouldUseBuiltInFunctions()
    {
        var variables = new Dictionary<string, JsonNode>
        {
            ["value_1"] = JsonValue.Create("VIP-user")
        };
        var valid = NeuCharWorkflowExpressionEngine.TryEvaluate(
            "if(contains(value_1, 'VIP'), substring(value_1, 0, 3), 'normal')",
            variables, out var result, out var error);
        Assert.IsTrue(valid, error);
        Assert.AreEqual("VIP", result!.GetValue<string>());
        Assert.IsFalse(NeuCharWorkflowExpressionEngine.TryValidate(
            "system.exit()", new[] { "value_1" }, out _));
    }

    [TestMethod]
    public void TemplateExpression_ShouldSupportDateAndStableArrayHelpers()
    {
        var variables = new Dictionary<string, JsonNode>
        {
            ["items"] = JsonNode.Parse("""
            [
                { "name": "second", "score": 2 },
                { "name": "first", "score": 3 },
                { "name": "third", "score": 2 }
            ]
            """)!
        };

        var valid = NeuCharWorkflowExpressionEngine.TryEvaluate(
            "orderBy(items, 'score', 'desc')[0].name",
            variables,
            out var sortedName,
            out var error);
        Assert.IsTrue(valid, error);
        Assert.AreEqual("first", sortedName!.GetValue<string>());
        Assert.IsTrue(NeuCharWorkflowExpressionEngine.TryEvaluate(
            "orderBy(items, 'score', 'desc')[1].name",
            variables,
            out var stableName,
            out error), error);
        Assert.AreEqual("second", stableName!.GetValue<string>());
        Assert.IsTrue(NeuCharWorkflowExpressionEngine.TryEvaluate(
            "now()",
            variables,
            out var timestamp,
            out error), error);
        Assert.IsTrue(DateTimeOffset.TryParse(timestamp!.GetValue<string>(), out _));
        Assert.IsFalse(NeuCharWorkflowExpressionEngine.TryValidate(
            "fetch('https://example.invalid')", new[] { "items" }, out _));
    }

    [TestMethod]
    public async Task WorkflowVariablesAndSafeCode_ShouldRemainRunLocalAndRequireDeclaration()
    {
        var engine = CreateEngine();
        const string graphJson = """
            {
              "variables": [
                { "name": "greeting", "value": "hello" },
                { "name": "shout", "value": "{{= upper(vars.greeting) }}" }
              ],
              "nodes": [
                { "id": "trigger", "type": "manual-trigger" },
                { "id": "code", "type": "code", "name": "更新变量", "config": {
                  "assignments": [{ "name": "greeting", "value": "{{= upper(input) }}" }]
                }},
                { "id": "end", "type": "end" }
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "code" },
                { "id": "edge-2", "source": "code", "target": "end" }
              ]
            }
            """;

        var graph = engine.ParseAndValidateGraph(graphJson);
        Assert.AreEqual(2, graph.Variables.Count);
        Assert.IsNull(await engine.ValidateReferencesAsync(graph));

        var outputs = new Dictionary<string, JsonNode>();
        var buildVariables = typeof(NeuCharWorkflowEngine).GetMethod(
            "BuildWorkflowVariables",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var values = (JsonObject)buildVariables.Invoke(null, new object[]
        {
            graph.Variables,
            JsonValue.Create("welcome"),
            outputs,
            new Dictionary<string, JsonNode>()
        })!;
        Assert.AreEqual("HELLO", values["shout"]!.GetValue<string>());

        var executeCode = typeof(NeuCharWorkflowEngine).GetMethod(
            "ExecuteCodeNode",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        executeCode.Invoke(null, new object[]
        {
            graph.Nodes.Single(node => node.Type == "code"),
            JsonValue.Create("welcome"),
            outputs,
            new Dictionary<string, JsonNode>()
        });
        Assert.AreEqual("WELCOME", values["greeting"]!.GetValue<string>());

        var invalidGraph = engine.ParseAndValidateGraph(
            graphJson.Replace(
                "\"assignments\": [{ \"name\": \"greeting\"",
                "\"assignments\": [{ \"name\": \"notDeclared\"",
                StringComparison.Ordinal));
        var invalidError = await engine.ValidateReferencesAsync(invalidGraph);
        StringAssert.Contains(invalidError, "已定义的工作流变量");
    }

    [TestMethod]
    public void TemplateExpressionBinding_ShouldValidateAndRenderWithoutDirectPlaceholder()
    {
        var validate = typeof(NeuCharWorkflowEngine).GetMethod("ValidateTemplateText", BindingFlags.NonPublic | BindingFlags.Static)!;
        var resolve = typeof(NeuCharWorkflowEngine).GetMethod("ResolveTemplate", BindingFlags.NonPublic | BindingFlags.Static)!;
        var template = JsonNode.Parse("""{ "$template": { "text": "{{= substring(value_1, 0, 3) }}", "bindings": [{ "token": "value_1", "source": { "nodeId": "source", "path": "$" } }] } }""")!;

        Assert.IsNull(validate.Invoke(null, new object[] { template["$template"]! }));
        var rendered = (JsonNode)resolve.Invoke(null, new object[]
        {
            template["$template"]!, JsonValue.Create("input"),
            new Dictionary<string, JsonNode> { ["source"] = JsonValue.Create("abcdef")! },
            new Dictionary<string, JsonNode>()
        })!;
        Assert.AreEqual("abc", rendered.GetValue<string>());
    }

    [TestMethod]
    public void TemplateExpressionConversions_ShouldPreserveWholeFormulaTypesAndRenderMixedText()
    {
        var variables = new Dictionary<string, JsonNode>
        {
            ["value_1"] = JsonValue.Create("42")!
        };

        Assert.IsTrue(NeuCharWorkflowExpressionEngine.TryEvaluate("toInt(value_1)", variables, out var intValue, out var intError), intError);
        Assert.AreEqual(42, intValue!.GetValue<int>());
        Assert.IsTrue(NeuCharWorkflowExpressionEngine.TryEvaluate("toLong('3000000000')", variables, out var longValue, out var longError), longError);
        Assert.AreEqual(3_000_000_000L, longValue!.GetValue<long>());
        Assert.IsTrue(NeuCharWorkflowExpressionEngine.TryEvaluate("toDecimal('42.5')", variables, out var decimalValue, out var decimalError), decimalError);
        Assert.AreEqual(42.5m, decimalValue!.GetValue<decimal>());
        Assert.IsTrue(NeuCharWorkflowExpressionEngine.TryEvaluate("toBool('1')", variables, out var boolValue, out var boolError), boolError);
        Assert.IsTrue(boolValue!.GetValue<bool>());
        Assert.IsTrue(NeuCharWorkflowExpressionEngine.TryEvaluate("toString(42)", variables, out var stringValue, out var stringError), stringError);
        Assert.AreEqual("42", stringValue!.GetValue<string>());
        Assert.IsFalse(NeuCharWorkflowExpressionEngine.TryEvaluate("toInt('not-a-number')", variables, out _, out var conversionError));
        StringAssert.Contains(conversionError, "无法将“not-a-number”转换为 Int32");

        var resolve = typeof(NeuCharWorkflowEngine).GetMethod("ResolveTemplate", BindingFlags.NonPublic | BindingFlags.Static)!;
        var typedTemplate = JsonNode.Parse("""{ "text": "{{= toInt(value_1) }}", "bindings": [{ "token": "value_1", "source": { "nodeId": "source", "path": "$" } }] }""")!;
        var typedResult = (JsonNode)resolve.Invoke(null, new object[]
        {
            typedTemplate, JsonValue.Create("input"),
            new Dictionary<string, JsonNode> { ["source"] = JsonValue.Create("42")! },
            new Dictionary<string, JsonNode>()
        })!;
        Assert.AreEqual(42, typedResult.GetValue<int>());

        var mixedTemplate = JsonNode.Parse("""{ "text": "编号 {{= toInt(value_1) }}", "bindings": [{ "token": "value_1", "source": { "nodeId": "source", "path": "$" } }] }""")!;
        var mixedResult = (JsonNode)resolve.Invoke(null, new object[]
        {
            mixedTemplate, JsonValue.Create("input"),
            new Dictionary<string, JsonNode> { ["source"] = JsonValue.Create("42")! },
            new Dictionary<string, JsonNode>()
        })!;
        Assert.AreEqual("编号 42", mixedResult.GetValue<string>());
    }

    [TestMethod]
    public void TemplateExpressionConversions_ShouldRejectInvalidLiteralDuringValidation()
    {
        var validate = typeof(NeuCharWorkflowEngine).GetMethod("ValidateTemplateText", BindingFlags.NonPublic | BindingFlags.Static)!;
        var error = (string?)validate.Invoke(null, new object[]
        {
            JsonNode.Parse("""{ "text": "{{= toInt('not-a-number') }}" }""")!
        });

        StringAssert.Contains(error, "无法将“not-a-number”转换为 Int32");
    }

    [TestMethod]
    public async Task ValidateReferencesAsync_TemplateObservedOutput_ShouldNotRequireReselectionAfterBindingReindex()
    {
        var engine = CreateEngine();
        const string graphJson = """
            {
              "nodes": [
                { "id": "trigger", "type": "manual-trigger", "name": "触发" },
                { "id": "stage", "type": "delay", "name": "阶段", "config": { "seconds": 1 } },
                { "id": "notify", "type": "neubell", "name": "发送纽铃", "config": {
                  "title": "提醒",
                  "summary": { "$template": {
                    "text": "只保留 {{value_2}}",
                    "bindings": [{ "token": "value_2", "source": {
                      "nodeId": "stage", "path": "$", "sourceKind": "observed-output"
                    }}]
                  }}
                }}
              ],
              "edges": [
                { "id": "edge-1", "source": "trigger", "target": "stage" },
                { "id": "edge-2", "source": "stage", "target": "notify" }
              ]
            }
            """;

        var error = await engine.ValidateReferencesAsync(engine.ParseAndValidateGraph(graphJson));

        Assert.IsNull(error);
    }

    [TestMethod]
    public void ObservedOutputSchema_ShouldExcludeSensitiveValuesAndMarkArrays()
    {
        var node = new NeuCharWorkflowNode { Id = "function-1", Name = "查询", Type = "function" };
        var schema = NeuCharWorkflowObservedOutputSchemaBuilder.Build(
            node,
            JsonNode.Parse("""{ "items": [{ "name": "first", "token": "hidden" }] }""")!);

        Assert.IsTrue(schema.Fields.Any(field => field.Path == "$.items.name" && field.RequiresIndex));
        Assert.IsFalse(schema.Fields.Any(field => field.Path.Contains("token")));
    }

    [TestMethod]
    public void RunCoordinator_ManualAbort_ShouldCompleteAsFailedWithManualAbortResult()
    {
        var runStateType = typeof(NeuCharWorkflowRunCoordinator).GetNestedType(
            "RunState",
            BindingFlags.NonPublic)!;
        var constructor = runStateType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();
        var state = constructor.Invoke(new object[] { Guid.NewGuid(), 12, 34, string.Empty, "manual" });
        var abortArguments = new object?[] { null };

        var accepted = (bool)runStateType.GetMethod("TryAbort")!.Invoke(state, abortArguments)!;
        var cancellationToken = (CancellationToken)runStateType.GetProperty("ManualAbortToken")!.GetValue(state)!;
        var result = (string?)runStateType.GetMethod("GetManualAbortResult")!.Invoke(state, null);
        runStateType.GetMethod("Complete")!.Invoke(state, new object?[] { false, "手动中止", "手动中止" });
        var snapshot = (NeuCharWorkflowRunSnapshot)runStateType.GetMethod("Snapshot")!.Invoke(state, new object[] { 0L })!;

        Assert.IsTrue(accepted);
        Assert.IsNull(abortArguments[0]);
        Assert.IsTrue(cancellationToken.IsCancellationRequested);
        Assert.AreEqual("手动中止", result);
        Assert.IsFalse(snapshot.Running);
        Assert.AreEqual(false, snapshot.Succeeded);
        Assert.AreEqual("手动中止", snapshot.FinalOutput);
        Assert.AreEqual("手动中止", snapshot.ErrorMessage);
    }

    [TestMethod]
    public void RunCoordinator_ShouldRetainLatest5000LiveEvents()
    {
        var runStateType = typeof(NeuCharWorkflowRunCoordinator).GetNestedType(
            "RunState",
            BindingFlags.NonPublic)!;
        var constructor = runStateType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();
        var state = constructor.Invoke(new object[] { Guid.NewGuid(), 12, 34, string.Empty, "manual" });
        var add = runStateType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var timestamp = DateTimeOffset.UtcNow;

        for (var index = 1; index <= 5_001; index++)
        {
            add.Invoke(state, new object[]
            {
                new NeuCharWorkflowProgress(
                    $"node-{index}",
                    "循环节点",
                    "success",
                    $"第 {index} 条",
                    string.Empty,
                    timestamp.AddTicks(index))
            });
        }

        var snapshot = (NeuCharWorkflowRunSnapshot)runStateType.GetMethod("Snapshot")!
            .Invoke(state, new object[] { 0L })!;

        Assert.AreEqual(5_000, snapshot.Events.Count);
        Assert.AreEqual(2L, snapshot.Events[0].Sequence);
        Assert.AreEqual(5_001L, snapshot.Events[^1].Sequence);
        Assert.AreEqual("第 2 条", snapshot.Events[0].Message);
    }

    private static Task<AppResponseBase<List<SampleOutput>>> ListOutputFunction() => null!;

    private sealed class SampleOutput
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    private sealed class CapturingWorkflowObjectProvider : IWorkflowObjectProvider
    {
        public string ProviderId => "test-provider";
        public WorkflowObjectExecutionRequest? LastRequest { get; private set; }

        public ValueTask<IReadOnlyList<WorkflowObjectDescriptor>> GetObjectsAsync(
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyList<WorkflowObjectDescriptor>>(
                new[]
                {
                    new WorkflowObjectDescriptor(
                        ProviderId,
                        "group:42",
                        "agent-group",
                        "审批组",
                        string.Empty,
                        true)
                });

        public ValueTask<WorkflowObjectExecutionResult> ExecuteAsync(
            WorkflowObjectExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return ValueTask.FromResult(new WorkflowObjectExecutionResult(true, "完成"));
        }
    }

    private static NeuCharWorkflowEngine CreateEngine() =>
        new(null!, null!, null!, null!, Array.Empty<IWorkflowObjectProvider>());
}
