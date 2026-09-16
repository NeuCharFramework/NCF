/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharPivotServiceTests.cs
    文件功能描述：NeuCharPivot AI 声明式布局安全规范化测试
----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System.Text.Json;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuCharPivotServiceTests
{
    [TestMethod]
    public void NormalizeLayout_UntrustedAiSchema_ShouldKeepCatalogBoundaryAndRequiredParameters()
    {
        var service = CreateService();
        var catalog = CreateCatalog();
        const string candidate =
            """
            ```json
            {
              "title": "<script>bad()</script> Quick panel",
              "columns": 99,
              "sections": [
                {
                  "title": "<b>Operations</b>",
                  "functions": [
                    {
                      "functionKey": "send-message",
                      "title": "<img src=x onerror=bad()>Send",
                      "accent": "javascript:bad",
                      "exposedParameters": ["optional", "unknown"]
                    },
                    { "functionKey": "internal-method", "title": "Do not allow" },
                    { "functionKey": "send-message", "title": "Duplicate" }
                  ]
                }
              ]
            }
            ```
            """;

        var layout = service.NormalizeLayout(candidate, catalog);
        var functions = layout.Sections.SelectMany(z => z.Functions).ToList();

        Assert.AreEqual(3, layout.Columns);
        Assert.IsFalse(layout.Title.Contains('<'));
        Assert.IsFalse(layout.Title.Contains('>'));
        Assert.AreEqual(catalog.Count, functions.Count);
        Assert.AreEqual(catalog.Count, functions.Select(z => z.FunctionKey).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.IsFalse(functions.Any(z => z.FunctionKey == "internal-method"));

        var send = functions.Single(z => z.FunctionKey == "send-message");
        Assert.AreEqual("blue", send.Accent);
        CollectionAssert.Contains(send.ExposedParameters, "requiredText");
        CollectionAssert.Contains(send.ExposedParameters, "optional");
        CollectionAssert.DoesNotContain(send.ExposedParameters, "unknown");

        Assert.IsTrue(functions.Any(z => z.FunctionKey == "health-check"),
            "AI 遗漏的 Function 必须由规范化器自动补回。" );
    }

    [TestMethod]
    public void NormalizeLayout_InvalidJson_ShouldBuildDeterministicFallback()
    {
        var layout = CreateService().NormalizeLayout("not-json", CreateCatalog());

        Assert.AreEqual(1, layout.Sections.Count);
        Assert.AreEqual(1, layout.Panels.Count);
        Assert.AreEqual("shortcuts", layout.Panels[0].Key);
        Assert.AreEqual("快捷操作", layout.Sections[0].Title);
        Assert.AreEqual(2, layout.Sections[0].Functions.Count);
    }

    [TestMethod]
    public void NormalizeLayout_PanelsAndRequiredOnlySelection_ShouldKeepMultiplePanelsAndUsableParameters()
    {
        const string candidate =
            """
            {
              "title": "Operations",
              "panels": [
                {
                  "key": "shortcuts",
                  "title": "快捷操作",
                  "type": "shortcuts",
                  "sections": [
                    {
                      "title": "发送",
                      "functions": [
                        {
                          "functionKey": "send-message",
                          "exposedParameters": ["requiredText"]
                        }
                      ]
                    }
                  ]
                },
                {
                  "key": "health",
                  "title": "健康状态",
                  "type": "summary",
                  "sections": [
                    {
                      "title": "检查",
                      "functions": [
                        { "functionKey": "health-check" }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        var layout = CreateService().NormalizeLayout(candidate, CreateCatalog());

        Assert.AreEqual(2, layout.Panels.Count);
        Assert.AreEqual("summary", layout.Panels[1].Type);
        var send = layout.Panels[0].Sections.SelectMany(section => section.Functions)
            .Single(function => function.FunctionKey == "send-message");
        CollectionAssert.Contains(send.ExposedParameters, "optional");
        Assert.AreEqual(2, layout.Panels.SelectMany(panel => panel.Sections)
            .SelectMany(section => section.Functions)
            .Select(function => function.FunctionKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count());
    }

    [TestMethod]
    public void GetCurrentParameterSchemaJson_ShouldRefreshLiveSelectionOptions()
    {
        var service = CreateService();
        var function = new Senparc.Areas.Admin.Domain.Models.DatabaseModel.NeuCharPivotFunction(
            1,
            "sandbox",
            "create",
            "创建沙箱",
            "按模板创建沙箱");
        function.Update(
            "创建沙箱",
            "按模板创建沙箱",
            """
            [
              {
                "name": "TemplateKey",
                "title": "模板",
                "required": true,
                "parameterType": 1,
                "options": [
                  { "value": "python", "text": "Python Exec" }
                ]
              }
            ]
            """,
            "{\"TemplateKey\":\"python\"}",
            "1.0.0",
            0,
            true);

        var descriptor = new NeuCharFunctionDescriptor(
            "sandbox",
            "Sandbox",
            "1.0.0",
            true,
            "create",
            "创建沙箱",
            "按模板创建沙箱",
            new[]
            {
                new FunctionParameterInfo
                {
                    Name = "Number1",
                    Title = "数字 1",
                    IsRequired = true,
                    ParameterType = ParameterType.Text,
                    SystemType = "Int32"
                },
                new FunctionParameterInfo
                {
                    Name = "Number2",
                    Title = "数字 2",
                    IsRequired = true,
                    ParameterType = ParameterType.Text,
                    SystemType = "Int32"
                },
                new FunctionParameterInfo
                {
                    Name = "TemplateKey",
                    Title = "模板",
                    IsRequired = true,
                    ParameterType = ParameterType.DropDownList,
                    SystemType = "String",
                    SelectionList = new SelectionList(
                        SelectionType.DropDownList,
                        new[]
                        {
                            new SelectionItem("python", "Python Exec"),
                            new SelectionItem("csharp", "C# Exec"),
                            new SelectionItem("jupyter-python", "JupyterLab Python"),
                            new SelectionItem("jupyter-csharp", "JupyterLab C#")
                        })
                },
                new FunctionParameterInfo
                {
                    Name = "Operator",
                    Title = "操作符",
                    IsRequired = true,
                    ParameterType = ParameterType.DropDownList,
                    SystemType = "String",
                    SelectionList = new SelectionList(
                        SelectionType.DropDownList,
                        new[]
                        {
                            new SelectionItem("+", "加"),
                            new SelectionItem("-", "减")
                        })
                }
            });

        var schemaJson = service.GetCurrentParameterSchemaJson(
            function,
            new Dictionary<string, NeuCharFunctionDescriptor>(StringComparer.OrdinalIgnoreCase)
            {
                ["create"] = descriptor
            });
        using var document = JsonDocument.Parse(schemaJson);
        var template = document.RootElement
            .EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "TemplateKey");
        var options = template.GetProperty("options");

        Assert.AreEqual(4, options.GetArrayLength());
        Assert.AreEqual("jupyter-csharp", options[3].GetProperty("value").GetString());
        Assert.IsTrue(schemaJson.Contains("\"name\":\"Number1\"", StringComparison.Ordinal));
        Assert.IsTrue(schemaJson.Contains("\"name\":\"Operator\"", StringComparison.Ordinal));
    }

    private static NeuCharPivotService CreateService() => new(null!, null!, null!, null!);

    private static IReadOnlyList<NeuCharFunctionDescriptor> CreateCatalog() =>
        new[]
        {
            new NeuCharFunctionDescriptor(
                "module-a",
                "Module A",
                "1.0.0",
                true,
                "send-message",
                "Send message",
                "Send a message",
                new[]
                {
                    new FunctionParameterInfo
                    {
                        Name = "requiredText",
                        Title = "Required text",
                        IsRequired = true,
                        ParameterType = ParameterType.Text,
                        SystemType = "String"
                    },
                    new FunctionParameterInfo
                    {
                        Name = "optional",
                        Title = "Optional",
                        ParameterType = ParameterType.Text,
                        SystemType = "String"
                    }
                }),
            new NeuCharFunctionDescriptor(
                "module-a",
                "Module A",
                "1.0.0",
                true,
                "health-check",
                "Health check",
                "Check module health",
                Array.Empty<FunctionParameterInfo>())
        };
}
