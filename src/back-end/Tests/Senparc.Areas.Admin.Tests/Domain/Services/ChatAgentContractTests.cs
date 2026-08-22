/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ChatAgentContractTests.cs
    文件功能描述：ChatAgent EventBus 公共契约与能力边界测试
----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.ChatAgent;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class ChatAgentContractTests
{
    [TestMethod]
    public void RequestAndResponse_ShouldUseTypedEventBusCorrelation()
    {
        var request = new ChatAgentRequestEvent(
            ChatAgentOperation.GenerateNeuCharPivot,
            "caller",
            "target",
            1,
            0,
            "Generate a panel");
        var response = new ChatAgentResponseEvent(
            request.RequestId,
            true,
            "{}",
            null,
            "ok");

        Assert.IsInstanceOfType<IIntegrationRequest<ChatAgentResponseEvent>>(request);
        Assert.IsInstanceOfType<IIntegrationResponse>(response);
        Assert.AreNotEqual(Guid.Empty, request.RequestId);
        Assert.AreEqual(request.RequestId, response.RequestId);
    }

    [TestMethod]
    public void PublicOperations_ShouldNotExposeArbitraryFunctionInvocation()
    {
        var names = Enum.GetNames<ChatAgentOperation>();

        CollectionAssert.AreEquivalent(
            new[] { "GenerateNeuCharPivot", "RefineNeuCharPivot" },
            names);
        Assert.IsFalse(names.Any(z => z.Contains("Invoke", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(names.Any(z => z.Contains("Execute", StringComparison.OrdinalIgnoreCase)));
    }
}
