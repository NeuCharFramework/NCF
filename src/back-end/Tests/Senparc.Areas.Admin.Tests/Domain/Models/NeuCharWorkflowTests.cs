/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowTests.cs
    文件功能描述：Workflow 自动保存设置与版本快照测试
----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.NeuCharWorkflow.ACL;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;
using Senparc.Xncf.NeuCharWorkflow.Models;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;

namespace Senparc.Areas.Admin.Tests.Domain.Models;

[TestClass]
public class NeuCharWorkflowTests
{
    [TestMethod]
    public void Update_ShouldPersistAutoSaveMinutesAndClampRange()
    {
        var workflow = new NeuCharWorkflow("Workflow", 1);

        Assert.AreEqual(3, workflow.AutoSaveMinutes);

        workflow.Update("Workflow", null, "{}", false, "manual", "{}", null, -1);
        Assert.AreEqual(0, workflow.AutoSaveMinutes);

        workflow.Update("Workflow", null, "{}", false, "manual", "{}", null, 9999);
        Assert.AreEqual(1440, workflow.AutoSaveMinutes);
        Assert.AreEqual(2, workflow.Revision);
    }

    [TestMethod]
    public void Version_ShouldCaptureWorkflowAndNormalizeSaveSource()
    {
        var workflow = new NeuCharWorkflow("Workflow", 1);
        workflow.Update("Workflow", "说明", "{\"nodes\":[]}", true, "manual", "{}", null, 5);

        var version = new NeuCharWorkflowVersion(workflow, 2, "SHORTCUT");

        Assert.AreEqual(workflow.Revision, version.Revision);
        Assert.AreEqual(workflow.GraphJson, version.GraphJson);
        Assert.AreEqual(5, version.AutoSaveMinutes);
        Assert.AreEqual(2, version.AdminUserId);
        Assert.AreEqual("shortcut", version.SaveSource);
    }

    [TestMethod]
    public async Task RuntimeStatusSave_ShouldNotOverwriteNewerDefinitionRevision()
    {
        var databaseName = $"NeuCharWorkflowTests-{Guid.NewGuid():N}";
        await using (var seedContext = CreateContext(databaseName))
        {
            var workflow = new WorkflowEntity("Workflow", 1);
            workflow.Update("Workflow", null, "{\"version\":1}", false, "manual", "{}", null, 3);
            seedContext.Add(workflow);
            await seedContext.SaveChangesAsync();
        }

        await using var runtimeContext = CreateContext(databaseName);
        var runtimeWorkflow = await runtimeContext.Set<WorkflowEntity>().SingleAsync();

        await using (var editorContext = CreateContext(databaseName))
        {
            var editedWorkflow = await editorContext.Set<WorkflowEntity>().SingleAsync();
            editedWorkflow.Update("Workflow", null, "{\"version\":2}", false, "manual", "{}", null, 3);
            await editorContext.SaveChangesAsync();
        }

        runtimeWorkflow.MarkStarted(null);
        Assert.IsNotNull(runtimeWorkflow.LastRunAt);
        var repository = new Mock<INeuCharWorkflowRepository>();
        repository.SetupGet(item => item.BaseDB).Returns(new TestDbData(runtimeContext)
        {
            ManualDetectChangeObject = true
        });
        var service = new NeuCharWorkflowService(
            repository.Object,
            new ServiceCollection().BuildServiceProvider());

        await service.SaveRuntimeStartedAsync(runtimeWorkflow);

        await using var verifyContext = CreateContext(databaseName);
        var savedWorkflow = await verifyContext.Set<WorkflowEntity>().SingleAsync();
        Assert.AreEqual(2, savedWorkflow.Revision);
        Assert.AreEqual("{\"version\":2}", savedWorkflow.GraphJson);
        Assert.IsNotNull(savedWorkflow.LastRunAt);
    }

    private static NeuCharWorkflowSenparcEntities_Sqlite CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<NeuCharWorkflowSenparcEntities_Sqlite>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new NeuCharWorkflowSenparcEntities_Sqlite(options);
    }

    private sealed class TestDbData(DbContext context) : NcfDbData
    {
        public override DbContext BaseDataContext => context;

        public override void CloseConnection()
        {
        }
    }
}
