/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceAssetPreviewBuilder.cs
    文件功能描述：WorkspaceAssetPreviewBuilder 相关定义与实现


    创建标识：Senparc - 20261024

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record WorkspaceHtmlPreview(string Html, string[] Warnings);

public sealed class WorkspaceAssetPreviewBuilder
{
    public const int MaxFileBytes = 2 * 1024 * 1024;
    public const int MaxHtmlBytes = 8 * 1024 * 1024;
    public const string ContentSecurityPolicy =
        "sandbox; default-src 'none'; script-src 'none'; img-src data:; " +
        "style-src 'unsafe-inline' data:; font-src data:; media-src data:; " +
        "base-uri 'none'; form-action 'none'; frame-ancestors 'none';";

    private static readonly Regex CssUrl = new(
        @"url\(\s*(['""]?)([^'""\)]+)\1\s*\)", RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1));
    private static readonly Regex CssImport = new(
        @"@import\s+(?:url\(\s*)?(['""]?)([^'""\s\);]+)\1\s*\)?([^;]*);",
        RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    private static readonly IReadOnlyDictionary<string, string> AssetTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".css"] = "text/css", [".png"] = "image/png", [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg", [".gif"] = "image/gif", [".webp"] = "image/webp",
            [".svg"] = "image/svg+xml", [".ico"] = "image/x-icon", [".avif"] = "image/avif",
            [".woff"] = "font/woff", [".woff2"] = "font/woff2", [".ttf"] = "font/ttf",
            [".otf"] = "font/otf"
        };

    public async Task<WorkspaceHtmlPreview> BuildAsync(
        IWorkspaceStorage storage, string workspaceId, string filePath,
        CancellationToken cancellationToken = default)
    {
        filePath = NormalizePath(filePath);
        if (!new[] { ".html", ".htm" }.Contains(Path.GetExtension(filePath).ToLowerInvariant()))
            throw new ArgumentException("只能在浏览器中预览或分享 HTML 文件。");

        var source = await storage.ReadFileBytesAsync(workspaceId, filePath, MaxFileBytes, cancellationToken);
        var document = new HtmlParser().ParseDocument(Encoding.UTF8.GetString(source.Content));
        var scope = filePath.Contains('/') ? filePath[..(filePath.LastIndexOf('/') + 1)] : "";
        var warnings = new HashSet<string>(StringComparer.Ordinal);
        var cache = new Dictionary<string, string>(StringComparer.Ordinal);
        var reading = new HashSet<string>(StringComparer.Ordinal);
        long totalBytes = source.Content.Length;
        var resourceCount = 0;

        async Task<string> AssetAsync(string reference, string sourcePath, int depth = 0)
        {
            if (string.IsNullOrWhiteSpace(reference) || reference.StartsWith('#')) return reference;
            if (reference.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                if (reference.Length > MaxFileBytes * 2) throw new InvalidOperationException("内嵌资源过大。");
                return reference;
            }
            var baseUri = new Uri("https://workspace.invalid/"
                + string.Join("/", sourcePath.Split('/').Select(Uri.EscapeDataString)));
            if (!Uri.TryCreate(baseUri, reference, out var resolved)
                || resolved.Scheme != "https" || resolved.Host != "workspace.invalid"
                || !string.IsNullOrEmpty(resolved.UserInfo) || resolved.Port != 443)
            {
                warnings.Add("已阻止外部资源，页面外观可能与原始页面不同。");
                return "";
            }

            var path = NormalizePath(Uri.UnescapeDataString(resolved.AbsolutePath.TrimStart('/')));
            if (!path.StartsWith(scope, StringComparison.Ordinal))
                throw new ArgumentException("静态资源必须位于 HTML 所在目录或其子目录。");
            if (!AssetTypes.TryGetValue(Path.GetExtension(path), out var type))
                throw new ArgumentException("不支持的静态资源类型。");
            if (cache.TryGetValue(path, out var value)) return value;
            if (depth > 8 || !reading.Add(path))
                throw new InvalidOperationException("样式资源循环引用或嵌套过深。");
            if (++resourceCount > 100) throw new InvalidOperationException("预览最多包含 100 个静态资源。");
            try
            {
                var file = await storage.ReadFileBytesAsync(workspaceId, path, MaxFileBytes, cancellationToken);
                totalBytes += file.Content.Length;
                if (totalBytes > MaxHtmlBytes) throw new InvalidOperationException("预览资源总大小超过 8 MB。");
                var bytes = type == "text/css"
                    ? Encoding.UTF8.GetBytes(await RewriteCssAsync(Encoding.UTF8.GetString(file.Content), path, depth + 1))
                    : file.Content;
                value = $"data:{type};base64,{Convert.ToBase64String(bytes)}";
                cache[path] = value;
                return value;
            }
            finally { reading.Remove(path); }
        }

        async Task<string> RewriteCssAsync(string css, string path, int depth = 0)
        {
            foreach (var match in CssImport.Matches(css).Cast<Match>().ToArray())
            {
                var asset = await AssetAsync(match.Groups[2].Value, path, depth);
                css = css.Replace(match.Value, $"@import url(\"{asset}\"){match.Groups[3].Value};", StringComparison.Ordinal);
            }
            foreach (var match in CssUrl.Matches(css).Cast<Match>().ToArray())
            {
                var asset = await AssetAsync(match.Groups[2].Value, path, depth);
                css = css.Replace(match.Value, $"url(\"{asset}\")", StringComparison.Ordinal);
            }
            return css;
        }

        // Fail closed: a snapshot must not silently omit a referenced file or include a wider directory.
        foreach (var element in document.QuerySelectorAll("script, iframe, object, embed, form, base, meta[http-equiv], animate, set, foreignObject"))
            element.Remove();
        foreach (var element in document.All.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var attribute in element.Attributes.ToArray())
            {
                if (attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase)
                    || attribute.Name is "srcset" or "srcdoc" or "action" or "formaction" or "ping"
                    || attribute.Name.Contains(':'))
                    element.RemoveAttribute(attribute.Name);
            }
            if (element.LocalName == "a")
            {
                var href = element.GetAttribute("href");
                if (href != null && !href.StartsWith('#')) element.RemoveAttribute("href");
                element.RemoveAttribute("target");
                element.RemoveAttribute("download");
            }
            else if (element.LocalName == "link")
            {
                if (!string.Equals(element.GetAttribute("rel"), "stylesheet", StringComparison.OrdinalIgnoreCase))
                {
                    element.Remove();
                    continue;
                }
                element.SetAttribute("href", await AssetAsync(element.GetAttribute("href") ?? "", filePath));
            }
            if (element.HasAttribute("src"))
                element.SetAttribute("src", await AssetAsync(element.GetAttribute("src"), filePath));
            if (element.HasAttribute("poster"))
                element.SetAttribute("poster", await AssetAsync(element.GetAttribute("poster"), filePath));
            if (element.HasAttribute("style"))
                element.SetAttribute("style", await RewriteCssAsync(element.GetAttribute("style"), filePath));
            if (element.LocalName == "style")
                element.TextContent = await RewriteCssAsync(element.TextContent, filePath);
        }
        var meta = document.CreateElement("meta");
        meta.SetAttribute("http-equiv", "Content-Security-Policy");
        meta.SetAttribute("content", ContentSecurityPolicy
            .Replace("sandbox; ", "", StringComparison.Ordinal)
            .Replace("frame-ancestors 'none';", "", StringComparison.Ordinal));
        document.Head.InsertBefore(meta, document.Head.FirstChild);
        var html = "<!doctype html>" + document.DocumentElement.OuterHtml;
        if (Encoding.UTF8.GetByteCount(html) > MaxHtmlBytes)
            throw new InvalidOperationException("生成的 HTML 快照超过 8 MB。");
        warnings.Add("只读预览：脚本、表单、外部网络资源及页面跳转已禁用。");
        return new WorkspaceHtmlPreview(html, warnings.ToArray());
    }

    public static string NormalizePath(string path, bool allowRoot = false)
    {
        if (path == null || path.Contains('\\') || path.StartsWith('/') || path.Contains(':')
            || path.Any(char.IsControl))
            throw new ArgumentException("必须使用 Workspace 内的相对路径。");
        var parts = path.Split('/');
        if (parts.Any(part => part is "." or "..") || parts.Any(part => part.Length == 0) && path.Length > 0)
            throw new ArgumentException("文件路径不允许包含空段或父目录。");
        if (!allowRoot && path.Length == 0) throw new ArgumentException("文件路径不能为空。");
        return path;
    }
}
