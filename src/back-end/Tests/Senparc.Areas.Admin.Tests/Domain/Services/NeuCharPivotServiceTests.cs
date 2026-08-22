/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharPivotServiceTests.cs
    文件功能描述：NeuCharPivot AI 声明式布局安全规范化测试
----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.XncfBase;

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
        Assert.AreEqual("快捷操作", layout.Sections[0].Title);
        Assert.AreEqual(2, layout.Sections[0].Functions.Count);
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
