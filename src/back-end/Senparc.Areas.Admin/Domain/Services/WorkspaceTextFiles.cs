/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceTextFiles.cs
    文件功能描述：识别 Workspace 中可预览、可编辑的文本与脚本文件


    创建标识：Senparc - 20261107

    修改标识：Senparc - 20261111
    修改描述：v0.12.28 完善 Workspace Agent、文件存储与模块注册兼容

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Senparc.Areas.Admin.Domain.Services;

public readonly record struct WorkspaceTextEdit(string Path, byte[] Content);

public static class WorkspaceTextFiles
{
    public const int MaxEditableBytes = 2 * 1024 * 1024;
    public const int PreviewWindowLines = 800;
    public const int PreviewWindowBytes = 384 * 1024;

    private static readonly Dictionary<string, string> Extensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".html"] = "text/html", [".htm"] = "text/html", [".md"] = "text/markdown",
            [".markdown"] = "text/markdown", [".mdx"] = "text/markdown",
            [".css"] = "text/css", [".scss"] = "text/css", [".sass"] = "text/css", [".less"] = "text/css",
            [".js"] = "text/javascript", [".mjs"] = "text/javascript", [".cjs"] = "text/javascript",
            [".jsx"] = "text/jsx",
            [".ts"] = "text/typescript", [".tsx"] = "text/typescript", [".mts"] = "text/typescript", [".cts"] = "text/typescript",
            [".json"] = "application/json", [".jsonc"] = "application/json", [".json5"] = "application/json",
            [".jsonl"] = "application/jsonl", [".ndjson"] = "application/jsonl",
            [".map"] = "application/json", [".ipynb"] = "application/json",
            [".svg"] = "image/svg+xml",
            [".txt"] = "text/plain", [".text"] = "text/plain", [".log"] = "text/plain",
            [".csv"] = "text/csv", [".tsv"] = "text/csv",
            [".xml"] = "text/xml", [".xsd"] = "text/xml", [".xsl"] = "text/xml", [".xslt"] = "text/xml",
            [".csproj"] = "text/xml", [".fsproj"] = "text/xml", [".vbproj"] = "text/xml",
            [".props"] = "text/xml", [".targets"] = "text/xml", [".config"] = "text/xml",
            [".yml"] = "text/yaml", [".yaml"] = "text/yaml",
            [".toml"] = "text/toml",
            [".ini"] = "text/plain", [".cfg"] = "text/plain", [".conf"] = "text/plain", [".properties"] = "text/plain",
            [".py"] = "text/x-python", [".pyw"] = "text/x-python", [".pyi"] = "text/x-python",
            [".cs"] = "text/x-csharp", [".csx"] = "text/x-csharp",
            [".cshtml"] = "text/x-razor", [".razor"] = "text/x-razor",
            [".vb"] = "text/x-vb", [".fs"] = "text/x-fsharp", [".fsx"] = "text/x-fsharp",
            [".java"] = "text/x-java", [".kt"] = "text/x-kotlin", [".kts"] = "text/x-kotlin", [".scala"] = "text/x-scala",
            [".go"] = "text/x-go", [".rs"] = "text/x-rust", [".rb"] = "text/x-ruby", [".php"] = "text/x-php",
            [".swift"] = "text/x-swift", [".dart"] = "text/x-dart",
            [".c"] = "text/x-c", [".h"] = "text/x-c",
            [".cpp"] = "text/x-c++", [".hpp"] = "text/x-c++", [".cc"] = "text/x-c++", [".cxx"] = "text/x-c++", [".hh"] = "text/x-c++",
            [".sh"] = "text/x-shellscript", [".bash"] = "text/x-shellscript", [".zsh"] = "text/x-shellscript", [".ksh"] = "text/x-shellscript",
            [".ps1"] = "text/x-powershell", [".psm1"] = "text/x-powershell",
            [".bat"] = "text/x-shellscript", [".cmd"] = "text/x-shellscript",
            [".sql"] = "text/x-sql",
            [".lua"] = "text/x-lua", [".r"] = "text/x-r", [".pl"] = "text/x-perl", [".pm"] = "text/x-perl",
            [".vue"] = "text/x-vue", [".gradle"] = "text/x-gradle",
            [".graphql"] = "text/plain", [".gql"] = "text/plain",
            [".env"] = "text/plain", [".gitignore"] = "text/plain", [".gitattributes"] = "text/plain",
            [".dockerignore"] = "text/plain", [".editorconfig"] = "text/plain", [".npmrc"] = "text/plain",
            [".nvmrc"] = "text/plain", [".python-version"] = "text/plain",
            [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif", [".webp"] = "image/webp", [".ico"] = "image/x-icon",
            [".pdf"] = "application/pdf"
        };

    private static readonly Dictionary<string, string> FileNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dockerfile"] = "text/x-dockerfile",
            ["Containerfile"] = "text/x-dockerfile",
            ["Makefile"] = "text/x-shellscript",
            ["GNUmakefile"] = "text/x-shellscript",
            ["Jenkinsfile"] = "text/x-shellscript",
            ["Rakefile"] = "text/x-ruby",
            ["Gemfile"] = "text/x-ruby",
            ["Procfile"] = "text/plain",
            ["Pipfile"] = "text/plain",
            ["LICENSE"] = "text/plain",
            ["COPYING"] = "text/plain",
            ["README"] = "text/plain",
            ["CHANGELOG"] = "text/plain",
            ["NOTICE"] = "text/plain"
        };

    public static string ResolveContentType(string relativePath)
    {
        var fileName = Path.GetFileName(relativePath ?? string.Empty);
        if (FileNames.TryGetValue(fileName, out var named)) return named;
        if (fileName.Equals(".env", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith(".env.", StringComparison.OrdinalIgnoreCase))
            return "text/plain";
        var extension = Path.GetExtension(fileName);
        if (extension.Length > 0 && Extensions.TryGetValue(extension, out var type)) return type;
        return extension.Length == 0 ? "text/plain" : "application/octet-stream";
    }

    public static bool IsText(string contentType) =>
        contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
        || contentType is "application/json" or "application/jsonl" or "image/svg+xml";

    public static bool CanEdit(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || IsBlocked(relativePath)) return false;
        return IsText(ResolveContentType(relativePath));
    }

    public static WorkspaceTextEdit PrepareEdit(string relativePath, string content)
    {
        if (content == null) throw new ArgumentException("文件内容不能为空。");
        if (content.Contains('\0')) throw new ArgumentException("文本文件不能包含二进制内容。");
        var path = WorkspaceAssetPreviewBuilder.NormalizePath(relativePath);
        if (!CanEdit(path)) throw new ArgumentException("该文件类型不支持在线编辑。");
        if (Encoding.UTF8.GetByteCount(content) > MaxEditableBytes)
            throw new ArgumentException("文本文件不能超过 2 MB。");
        return new WorkspaceTextEdit(path, Encoding.UTF8.GetBytes(content));
    }

    private static bool IsBlocked(string relativePath)
    {
        var parts = relativePath.Split('/');
        if (parts.Any(part => part.Equals(".git", StringComparison.OrdinalIgnoreCase)
            || part.Equals(".workspace", StringComparison.OrdinalIgnoreCase)))
            return true;
        var name = parts[^1];
        return name.Equals(".workspace.json", StringComparison.OrdinalIgnoreCase);
    }
}
