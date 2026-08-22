using System.Text.Json;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuCharWorkflowWebhookConfigTests
{
    [TestMethod]
    public void Normalize_ShouldGenerateTokenAndKeepParameterDefinitions()
    {
        var config = NeuCharWorkflowWebhookConfig.Normalize(
            """
            {
              "method": "POST",
              "parameters": [
                { "name": "userId", "required": true, "description": "用户标识" }
              ]
            }
            """);

        Assert.AreEqual("post", config.Method);
        Assert.IsFalse(string.IsNullOrWhiteSpace(config.Token));
        Assert.AreEqual(1, config.Parameters.Count);
        Assert.AreEqual("userId", config.Parameters[0].Name);
        Assert.IsTrue(config.Parameters[0].Required);
    }

    [TestMethod]
    public void Normalize_ShouldPreserveExistingTokenWhenEditing()
    {
        var config = NeuCharWorkflowWebhookConfig.Normalize(
            "{\"method\":\"get\",\"parameters\":[]}",
            "{\"method\":\"post\",\"token\":\"existing-secret\"}");

        Assert.AreEqual("get", config.Method);
        Assert.AreEqual("existing-secret", config.Token);
    }

    [TestMethod]
    public void Normalize_ShouldRejectUnsupportedMethodAndDuplicateParameter()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
            NeuCharWorkflowWebhookConfig.Normalize("{\"method\":\"delete\"}"));
        Assert.ThrowsException<InvalidOperationException>(() =>
            NeuCharWorkflowWebhookConfig.Normalize(
                "{\"parameters\":[{\"name\":\"id\"},{\"name\":\"ID\"}]}"));
    }

    [TestMethod]
    public void ToJson_ShouldUseStableCamelCaseShape()
    {
        var config = NeuCharWorkflowWebhookConfig.Normalize("{\"method\":\"any\"}");
        using var json = JsonDocument.Parse(config.ToJson());
        Assert.IsTrue(json.RootElement.TryGetProperty("method", out _));
        Assert.IsTrue(json.RootElement.TryGetProperty("token", out _));
        Assert.IsTrue(json.RootElement.TryGetProperty("parameters", out _));
    }
}
