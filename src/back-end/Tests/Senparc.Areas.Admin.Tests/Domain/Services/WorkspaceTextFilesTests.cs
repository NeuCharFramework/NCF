using System.Text;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class WorkspaceTextFilesTests
{
    [TestMethod]
    public void RecognizesScriptsAndCommonText()
    {
        Assert.AreEqual("text/x-python", WorkspaceTextFiles.ResolveContentType("src/main.py"));
        Assert.AreEqual("text/javascript", WorkspaceTextFiles.ResolveContentType("app.js"));
        Assert.AreEqual("text/typescript", WorkspaceTextFiles.ResolveContentType("app.ts"));
        Assert.AreEqual("text/x-csharp", WorkspaceTextFiles.ResolveContentType("Program.cs"));
        Assert.AreEqual("text/x-shellscript", WorkspaceTextFiles.ResolveContentType("deploy.sh"));
        Assert.AreEqual("text/yaml", WorkspaceTextFiles.ResolveContentType("config.yml"));
        Assert.AreEqual("text/markdown", WorkspaceTextFiles.ResolveContentType("docs/guide.markdown"));
        Assert.AreEqual("text/x-dockerfile", WorkspaceTextFiles.ResolveContentType("Dockerfile"));
        Assert.AreEqual("text/plain", WorkspaceTextFiles.ResolveContentType(".gitignore"));
        Assert.AreEqual("text/plain", WorkspaceTextFiles.ResolveContentType(".env.local"));
        Assert.AreEqual("text/plain", WorkspaceTextFiles.ResolveContentType("README"));
        Assert.AreEqual("application/jsonl", WorkspaceTextFiles.ResolveContentType("events.jsonl"));
        Assert.AreEqual("application/jsonl", WorkspaceTextFiles.ResolveContentType("logs/run.ndjson"));
        Assert.IsTrue(WorkspaceTextFiles.IsText(WorkspaceTextFiles.ResolveContentType("events.jsonl")));
        Assert.IsTrue(WorkspaceTextFiles.CanEdit("events.jsonl"));
        Assert.AreEqual("image/png", WorkspaceTextFiles.ResolveContentType("image.png"));
        Assert.AreEqual("application/octet-stream", WorkspaceTextFiles.ResolveContentType("bin/app.exe"));

        Assert.IsTrue(WorkspaceTextFiles.CanEdit("src/main.py"));
        Assert.IsTrue(WorkspaceTextFiles.CanEdit("notes.txt"));
        Assert.IsTrue(WorkspaceTextFiles.CanEdit("README"));
        Assert.IsFalse(WorkspaceTextFiles.CanEdit("image.png"));
        Assert.IsFalse(WorkspaceTextFiles.CanEdit(".git/config"));
        Assert.IsFalse(WorkspaceTextFiles.CanEdit(".workspace.json"));
    }

    [TestMethod]
    public void PrepareEditRejectsBinaryTraversalAndNullBytes()
    {
        var edit = WorkspaceTextFiles.PrepareEdit("src/main.py", "print('ok')\n");
        Assert.AreEqual("src/main.py", edit.Path);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("print('ok')\n"), edit.Content);

        Assert.ThrowsException<ArgumentException>(() => WorkspaceTextFiles.PrepareEdit("../main.py", "x"));
        Assert.ThrowsException<ArgumentException>(() => WorkspaceTextFiles.PrepareEdit("a.png", "x"));
        Assert.ThrowsException<ArgumentException>(() => WorkspaceTextFiles.PrepareEdit("a.py", "a\0b"));
        Assert.ThrowsException<ArgumentException>(() => WorkspaceTextFiles.PrepareEdit("a.py", null!));
    }
}
