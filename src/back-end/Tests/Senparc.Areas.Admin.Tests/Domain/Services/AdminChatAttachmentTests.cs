using System.IO.Compression;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.OHS.Local.Controllers;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Config;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.Workspace;
using Senparc.Xncf.Workspace.Abstractions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatAttachmentTests
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);
    private Mock<IWorkspaceStorage> _storage = null!;
    private ServiceProvider _services = null!;
    private AdminChatSession _session = null!;
    private WorkspaceDocumentReader _reader = null!;
    private AdminChatAttachmentService _attachments = null!;
    private int _ownerId = 7;

    [TestInitialize]
    public void Initialize()
    {
        _session = new AdminChatSession("Attachments", 7, "ws_test");
        typeof(AdminChatSession).GetProperty("Id")!.SetValue(_session, 42);
        _storage = new Mock<IWorkspaceStorage>();
        _storage.Setup(storage => storage.GetAsync("ws_test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new WorkspaceDescriptor("ws_test", _ownerId, "Test", "/private", DateTime.UtcNow, DateTime.UtcNow));
        _storage.Setup(storage => storage.CreateDirectoryAsync("ws_test", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storage.Setup(storage => storage.WriteFileBytesAsync("ws_test", It.IsAny<WorkspaceWriteFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, WorkspaceWriteFileRequest request, CancellationToken _) =>
            {
                if (_files.ContainsKey(request.RelativePath) && !request.Overwrite) throw new IOException("Already exists.");
                _files[request.RelativePath] = request.Content.ToArray();
                return new WorkspaceWriteFileResult(Metadata(request.RelativePath), true);
            });
        _storage.Setup(storage => storage.GetFileInfoAsync("ws_test", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string path, CancellationToken _) => Metadata(path));
        _storage.Setup(storage => storage.ReadFileBytesAsync("ws_test", It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string path, int maxBytes, CancellationToken _) =>
            {
                var metadata = Metadata(path);
                if (metadata.Length > maxBytes) throw new InvalidOperationException("Read limit.");
                return new WorkspaceFileContent(metadata, _files[path].ToArray());
            });
        _storage.Setup(storage => storage.DeleteEntryAsync("ws_test", It.IsAny<WorkspaceDeleteEntryRequest>(), It.IsAny<CancellationToken>()))
            .Returns((string _, WorkspaceDeleteEntryRequest request, CancellationToken _) =>
            {
                foreach (var key in _files.Keys.Where(key => key.StartsWith(request.RelativePath + "/", StringComparison.Ordinal)).ToArray())
                    _files.Remove(key);
                return Task.CompletedTask;
            });
        var repository = new Mock<IAdminChatSessionRepository>();
        repository.Setup(value => value.GetFirstOrDefaultObjectAsync(
                It.IsAny<Expression<Func<AdminChatSession, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<AdminChatSession, bool>> predicate, string[] _) =>
                predicate.Compile()(_session) ? _session : null);
        _services = new ServiceCollection().AddLogging()
            .AddSingleton(_storage.Object)
            .AddSingleton<IWorkspaceClientFactory>(new WorkspaceClientFactory(_storage.Object))
            .AddSingleton<IOptions<WorkspaceOptions>>(Options.Create(new WorkspaceOptions()))
            .AddSingleton<WorkspaceDocumentReader>()
            .AddSingleton(provider => new AdminChatSessionService(repository.Object, provider))
            .AddSingleton<AdminChatAttachmentService>().BuildServiceProvider();
        _reader = _services.GetRequiredService<WorkspaceDocumentReader>();
        _attachments = _services.GetRequiredService<AdminChatAttachmentService>();
    }

    [TestCleanup]
    public void Cleanup() => _services.Dispose();

    [TestMethod]
    public async Task Upload_PreservesOriginalAndAllowsExplicitUserUploadInReadOnlyAiSession()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello 中文\nSecond paragraph.");
        var uploaded = await _attachments.UploadAsync(_session, Form(bytes, "资料.txt"), null!, default);
        Assert.IsFalse(_session.WorkspaceWriteEnabled);
        CollectionAssert.AreEqual(bytes, _files[uploaded.RelativePath]);
        Assert.IsTrue(uploaded.RelativePath.StartsWith("uploads/", StringComparison.Ordinal));
        Assert.AreEqual("资料.txt", uploaded.Name);
        Assert.AreEqual("已本地解析", uploaded.LocalStatus);
        Assert.AreEqual(bytes.Length, uploaded.Size);
        Assert.IsFalse(uploaded.UseVision);
        var message = new AdminChatMessage(42, ChatMessageRoleType.User, "Read the file", 1);
        message.SetAttachments([uploaded]);
        Assert.AreEqual(uploaded.ETag, message.GetAttachments().Single().ETag);
        Assert.IsFalse(message.AttachmentsJson.Contains("Second paragraph.", StringComparison.Ordinal));
        Assert.IsFalse(message.AttachmentsJson.Contains(Convert.ToBase64String(bytes), StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Upload_RejectsScriptsMacroExtensionsWrongSignaturesAndEmptyFiles()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _attachments.UploadAsync(_session, Form("print('x')"u8.ToArray(), "run.py"), null!, default));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _attachments.UploadAsync(_session, Form("macro"u8.ToArray(), "book.xlsm"), null!, default));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() =>
            _attachments.UploadAsync(_session, Form("not pdf"u8.ToArray(), "fake.pdf"), null!, default));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _attachments.UploadAsync(_session, Form([], "empty.txt"), null!, default));
        Assert.AreEqual(0, _files.Count);
    }

    [TestMethod]
    public async Task Validation_RejectsForeignOwnershipChangedFilesDuplicatesAndTooManyAttachments()
    {
        var uploaded = await UploadTextAsync("one.txt", "Original");
        _ownerId = 8;
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => ValidateAsync(uploaded));
        _ownerId = 7;
        _files[uploaded.RelativePath] = "Changed"u8.ToArray();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => ValidateAsync(uploaded));
        _files[uploaded.RelativePath] = "Original"u8.ToArray();
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _attachments.ValidateAsync(_session, [Input(uploaded), Input(uploaded)]));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _attachments.ValidateAsync(_session, Enumerable.Repeat(Input(uploaded), 5).ToArray()));
        var invalid = Input(uploaded);
        invalid.RelativePath = "../outside.txt";
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _attachments.ValidateAsync(_session, [invalid]));
    }

    [TestMethod]
    public async Task Vision_RequiresExplicitConsentAndUsesTypedImageContent_NotBase64PromptText()
    {
        var jpeg = TinyJpeg();
        var uploaded = await _attachments.UploadAsync(_session, Form(jpeg, "photo.jpg"), Form(jpeg, "vision.jpg"), default);
        Assert.IsTrue(uploaded.CanUseVision);
        var references = await ValidateAsync(uploaded);
        var sessions = _services.GetRequiredService<AdminChatSessionService>();
        var plain = await _attachments.BuildVisionMessagesAsync(42, 7, "Question", references, sessions, default);
        Assert.AreEqual(0, plain.SelectMany(message => message.Contents).OfType<DataContent>().Count());
        uploaded.UseVision = true;
        references = await ValidateAsync(uploaded);
        var vision = await _attachments.BuildVisionMessagesAsync(42, 7, "Question", references, sessions, default);
        var image = vision.SelectMany(message => message.Contents).OfType<DataContent>().Single();
        Assert.AreEqual("image/jpeg", image.MediaType);
        CollectionAssert.AreEqual(jpeg, image.Data.ToArray());
        Assert.IsFalse(string.Concat(vision.SelectMany(message => message.Contents).OfType<TextContent>()
            .Select(text => text.Text)).Contains(Convert.ToBase64String(jpeg), StringComparison.Ordinal));
        _files[uploaded.RelativePath[..uploaded.RelativePath.LastIndexOf('/')] + "/.vision.jpg"] = TinyJpeg(2);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            _attachments.BuildVisionMessagesAsync(42, 7, "Question", references, sessions, default));
    }

    [TestMethod]
    public void Vision_EnforcesPreviewByteAndPixelLimits()
    {
        AdminChatAttachmentService.ValidateVisionJpeg(TinyJpeg());
        Assert.ThrowsException<ArgumentException>(() => AdminChatAttachmentService.ValidateVisionJpeg(TinyJpeg(2000)));
        Assert.ThrowsException<ArgumentException>(() =>
            AdminChatAttachmentService.ValidateVisionJpeg(new byte[AdminChatAttachmentService.MaxVisionBytes + 1]));
        Assert.ThrowsException<InvalidDataException>(() => AdminChatAttachmentService.ValidateVisionJpeg("fake image"u8.ToArray()));
    }

    [TestMethod]
    public async Task Upload_RollsBackOriginalIfPreviewPersistenceFails()
    {
        _storage.Setup(storage => storage.WriteFileBytesAsync("ws_test",
                It.Is<WorkspaceWriteFileRequest>(request => request.RelativePath.EndsWith("/.vision.jpg")),
                It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("Preview write failed."));
        await Assert.ThrowsExceptionAsync<IOException>(() =>
            _attachments.UploadAsync(_session, Form(TinyJpeg(), "photo.jpg"), Form(TinyJpeg(), "vision.jpg"), default));
        Assert.AreEqual(0, _files.Count);
    }

    [TestMethod]
    public async Task Office_WordTextAndExcelNamesSparseCellsAndFormulaCachesArePreserved()
    {
        var word = Office(("word/document.xml",
            "<w:document xmlns:w='urn:word'><w:body><w:p><w:r><w:t>Hello 中文</w:t></w:r></w:p></w:body></w:document>"));
        var doc = await _reader.ParseAsync(word, ".docx", default);
        StringAssert.Contains(doc.Sections[0].Text, "Hello 中文");
        StringAssert.Contains(doc.Sections[0].Title, "非物理页码");
        var excel = Office(
            ("xl/workbook.xml", "<workbook xmlns:r='urn:rel'><sheets><sheet name='销售' r:id='r2'/><sheet name='价格' r:id='r1'/></sheets></workbook>"),
            ("xl/_rels/workbook.xml.rels", "<Relationships><Relationship Id='r1' Target='worksheets/sheet1.xml'/><Relationship Id='r2' Target='worksheets/sheet2.xml'/></Relationships>"),
            ("xl/sharedStrings.xml", "<sst><si><t>金额</t></si></sst>"),
            ("xl/worksheets/sheet2.xml", "<worksheet><sheetData><row r='1'><c r='A1' t='s'><v>0</v></c><c r='C1'><v>42</v></c><c r='D1'><f>SUM(C1:C2)</f><v>100</v></c></row></sheetData></worksheet>"),
            ("xl/worksheets/sheet1.xml", "<worksheet><sheetData><row><c r='B1' t='inlineStr'><is><t>Second</t></is></c></row></sheetData></worksheet>"));
        var workbook = await _reader.ParseAsync(excel, ".xlsx", default);
        Assert.AreEqual("销售", workbook.Sections[0].Title);
        Assert.AreEqual("价格", workbook.Sections[1].Title);
        StringAssert.Contains(workbook.Sections[0].Text, "A1=金额\tC1=42");
        StringAssert.Contains(workbook.Sections[0].Text, "=SUM(C1:C2) [缓存值: 100]");
        StringAssert.Contains(workbook.Notice, "不重新计算");
    }

    [TestMethod]
    public async Task Office_PresentationOrderUsesRelationships_NotPartFilenameOrder()
    {
        var slides = Office(
            ("ppt/presentation.xml", "<presentation xmlns:r='urn:rel'><sldIdLst><sldId id='1' r:id='r2'/><sldId id='2' r:id='r1'/></sldIdLst></presentation>"),
            ("ppt/_rels/presentation.xml.rels", "<Relationships><Relationship Id='r1' Target='slides/slide1.xml'/><Relationship Id='r2' Target='slides/slide2.xml'/></Relationships>"),
            ("ppt/slides/slide1.xml", "<slide><p><t>Last slide</t></p></slide>"),
            ("ppt/slides/slide2.xml", "<slide><p><t>First slide</t></p></slide>"));
        var result = await _reader.ParseAsync(slides, ".pptx", default);
        Assert.AreEqual("First slide", result.Sections[0].Text);
        Assert.AreEqual("Last slide", result.Sections[1].Text);
    }

    [TestMethod]
    public async Task Office_RejectsDtdAndOversizedArchiveEntries()
    {
        var dtd = Office(("word/document.xml",
            "<!DOCTYPE document [<!ENTITY secret SYSTEM 'file:///never-read'>]><document><p><t>&secret;</t></p></document>"));
        await Assert.ThrowsExceptionAsync<System.Xml.XmlException>(() => _reader.ParseAsync(dtd, ".docx", default));
        var bomb = Office(("word/document.xml", new string(' ', 8 * 1024 * 1024 + 1)));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => _reader.ParseAsync(bomb, ".docx", default));
    }

    [TestMethod]
    public async Task Pdf_ExtractsPerPageAndClearlyIdentifiesScanOnlyPages()
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(PageSize.A4);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("PDF local text", 12, new PdfPoint(25, 700), font);
        builder.AddPage(PageSize.A4);
        var result = await _reader.ParseAsync(builder.Build(), ".pdf", default);
        Assert.AreEqual(2, result.Sections.Count);
        StringAssert.Contains(result.Sections[0].Text, "PDF local text");
        Assert.AreEqual("", result.Sections[1].Text.Trim());
        StringAssert.Contains(result.Notice, "未执行 OCR");
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() =>
            _reader.ParseAsync("%PDF-corrupt"u8.ToArray(), ".pdf", default));
    }

    [TestMethod]
    public async Task DocumentTools_PageAndSearchWithinSharedContextBudget_AndNeverReadBinaryAsText()
    {
        var uploaded = await UploadTextAsync("long.txt", new string('a', 12000) + "\nneedle");
        await using var client = _services.GetRequiredService<IWorkspaceClientFactory>().Create(
            new WorkspaceAccessContext("ws_test", new WorkspacePolicy(AllowRead: true), "Test", "7"));
        var factory = typeof(AdminChatAiService).Assembly.GetType("Senparc.Areas.Admin.Domain.Services.AdminChatWorkspaceToolFactory")!;
        var tools = (IReadOnlyList<AIFunction>)factory.GetMethod("Create")!.Invoke(null, [client, false, false, _reader])!;
        var search = tools.Single(tool => tool.Name == "workspace_document_search");
        var found = (await search.InvokeAsync(new AIFunctionArguments
            { ["relativePath"] = uploaded.RelativePath, ["query"] = "needle" }))!.ToString()!;
        StringAssert.Contains(found, "needle");
        var read = tools.Single(tool => tool.Name == "workspace_document_read");
        var used = found.Length;
        var exhausted = false;
        for (var index = 0; index < 10; index++)
        {
            try
            {
                var output = (await read.InvokeAsync(new AIFunctionArguments
                    { ["relativePath"] = uploaded.RelativePath, ["section"] = 1, ["maxCharacters"] = 4000 }))!.ToString()!;
                used += output.Length;
                using var json = JsonDocument.Parse(output);
                Assert.IsTrue(json.RootElement.GetProperty("hasMore").GetBoolean());
                Assert.AreEqual(4000, json.RootElement.GetProperty("nextOffset").GetInt32());
            }
            catch (InvalidOperationException)
            {
                exhausted = true;
                break;
            }
        }
        Assert.IsTrue(exhausted);
        Assert.IsTrue(used <= 16000);
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await tools.Single(tool => tool.Name == "workspace_read")
            .InvokeAsync(new AIFunctionArguments { ["relativePath"] = "binary.pdf" }));
    }

    [TestMethod]
    public void Prompt_ContainsOnlyReferencesAndDoesNotDuplicateCurrentUserMessage()
    {
        var reference = new AdminChatAttachment
        {
            Name = "资料.xlsx", RelativePath = "uploads/abc/资料.xlsx", ETag = "etag",
            SectionCount = 2, Notice = "仅缓存值"
        };
        var current = new AdminChatMessage(42, ChatMessageRoleType.User, "unique-current-question", 1);
        current.SetAttachments([reference]);
        var prompt = AdminChatPromptBuilder.BuildUserPrompt([current], current.Content, [reference]);
        Assert.AreEqual(1, prompt.Split("unique-current-question").Length - 1);
        StringAssert.Contains(prompt, "uploads/abc/资料.xlsx");
        StringAssert.Contains(prompt, "不代表已读取全文");
        Assert.IsTrue(prompt.Length <= AdminChatPromptBuilder.MaxUserPromptCharacters);
    }

    [TestMethod]
    public void Migrations_AllSixProvidersOnlyAddNullableAttachmentReferences()
    {
        var types = typeof(AdminChatMessage).Assembly.GetTypes()
            .Where(type => type.Name == "AddAdminChatAttachments" && typeof(Migration).IsAssignableFrom(type)).ToArray();
        Assert.AreEqual(6, types.Length);
        foreach (var type in types)
        {
            var migration = (Migration)Activator.CreateInstance(type)!;
            var operation = (AddColumnOperation)migration.UpOperations.Single();
            Assert.AreEqual("AttachmentsJson", operation.Name);
            Assert.AreEqual("ADMIN_AdminChatMessage", operation.Table);
            Assert.IsTrue(operation.IsNullable);
        }
    }

    [TestMethod]
    public void DesignTimeConfiguration_UsesActualWebDirectory_NotStartupExample()
    {
        var root = Senparc.Areas.Admin.Domain.Models.SenparcDbContextFactoryConfig.RootDirectoryPath;
        Assert.AreEqual("Senparc.Web", Path.GetFileName(root));
        Assert.IsTrue(File.Exists(Path.Combine(root, "appsettings.json")));
        Assert.IsFalse(root.Contains("DatabasePlant", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task HttpUpload_RequiresLoginAndAntiforgery_AndReturnsCanonicalPrivateReference()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.ContentRootPath).Returns(Path.Combine(Path.GetTempPath(), "unused-share-" + Guid.NewGuid()));
        using var shares = new WorkspaceAssetShareStore(environment.Object, new EphemeralDataProtectionProvider(),
            NullLogger<WorkspaceAssetShareStore>.Instance);
        var assetService = new AdminChatWorkspaceAssetService(_services.GetRequiredService<AdminChatSessionService>(),
            _services, new WorkspaceAssetPreviewBuilder());
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(assetService).AddSingleton(shares).AddSingleton(_attachments);
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(AdminChatWorkspaceAssetsController).Assembly);
        builder.Services.AddAntiforgery();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAuthentication(SiteConfig.NcfAdminAuthorizeScheme)
            .AddScheme<AuthenticationSchemeOptions, WorkspaceAssetTests.FixtureAuthenticationHandler>(SiteConfig.NcfAdminAuthorizeScheme, _ => { })
            .AddScheme<AuthenticationSchemeOptions, WorkspaceAssetTests.FixtureAuthenticationHandler>(AdminOrJwtAuthorizeAttribute.JwtScheme, _ => { });
        builder.Services.AddAuthorization(options => options.AddPolicy(NcfAuthorizationPolicyNames.AdminOnly,
            policy => policy.RequireAuthenticatedUser()));
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapGet("/fixture/token", (HttpContext context, IAntiforgery antiforgery) =>
            new { token = antiforgery.GetAndStoreTokens(context).RequestToken });
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer() }) { BaseAddress = new Uri(address) };
            const string root = "/api/Senparc.Areas.Admin/WorkspaceAssets/42/attachments";
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync(root + "/capabilities")).StatusCode);
            client.DefaultRequestHeaders.Add("X-Fixture-User", "7");
            var bytes = Encoding.UTF8.GetBytes("Local document content");
            using var missingCsrf = await client.PostAsync(root, Multipart(bytes, "report.txt"));
            Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
            using var token = JsonDocument.Parse(await client.GetStringAsync("/fixture/token"));
            client.DefaultRequestHeaders.Add("RequestVerificationToken", token.RootElement.GetProperty("token").GetString());
            using var response = await client.PostAsync(root, Multipart(bytes, "report.txt"));
            var json = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, json);
            using var result = JsonDocument.Parse(json);
            var attachment = result.RootElement.GetProperty("attachment");
            Assert.AreEqual("report.txt", attachment.GetProperty("name").GetString());
            Assert.AreEqual("已本地解析", attachment.GetProperty("localStatus").GetString());
            Assert.IsFalse(attachment.GetProperty("useVision").GetBoolean());
            Assert.IsFalse(attachment.GetProperty("canUseVision").GetBoolean());
            CollectionAssert.AreEqual(bytes, _files[attachment.GetProperty("relativePath").GetString()!]);
            Assert.IsFalse(string.IsNullOrEmpty(attachment.GetProperty("eTag").GetString()));
            using var badType = await client.PostAsync(root, Multipart(bytes, "script.py"));
            Assert.AreEqual(HttpStatusCode.BadRequest, badType.StatusCode);
            using var wrongSession = await client.PostAsync(root.Replace("/42/", "/43/"), Multipart(bytes, "report.txt"));
            Assert.AreEqual(HttpStatusCode.NotFound, wrongSession.StatusCode);
        }
        finally { await app.StopAsync(); }
    }

    private static MultipartFormDataContent Multipart(byte[] bytes, string name)
    {
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(bytes), "file", name);
        return content;
    }

    private Task<AdminChatAttachment> UploadTextAsync(string name, string text) =>
        _attachments.UploadAsync(_session, Form(Encoding.UTF8.GetBytes(text), name), null!, default);
    private Task<IReadOnlyList<AdminChatAttachment>> ValidateAsync(AdminChatAttachment item) =>
        _attachments.ValidateAsync(_session, [Input(item)]);
    private static AdminChatAttachmentInput Input(AdminChatAttachment item) => new()
    {
        RelativePath = item.RelativePath, ETag = item.ETag, VisionETag = item.VisionETag, UseVision = item.UseVision
    };
    private static IFormFile Form(byte[] bytes, string name) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name)
    {
        Headers = new HeaderDictionary(), ContentType = "application/octet-stream"
    };
    private WorkspaceFileMetadata Metadata(string path)
    {
        if (!_files.TryGetValue(path, out var bytes)) throw new FileNotFoundException("Missing.");
        return new WorkspaceFileMetadata(path, false, bytes.Length, DateTime.UtcNow, DateTime.UtcNow,
            Convert.ToHexString(SHA256.HashData(bytes)));
    }
    private static byte[] TinyJpeg(int width = 1) =>
        [0xff, 0xd8, 0xff, 0xc0, 0, 17, 8, 0, 1, (byte)(width >> 8), (byte)width, 3, 1, 0x11, 0, 2, 0x11, 0, 3, 0x11, 0, 0xff, 0xd9];
    private static byte[] Office(params (string Path, string Content)[] entries)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
            foreach (var entry in entries)
            {
                using var writer = new StreamWriter(archive.CreateEntry(entry.Path).Open(), Encoding.UTF8);
                writer.Write(entry.Content);
            }
        return memory.ToArray();
    }
}
