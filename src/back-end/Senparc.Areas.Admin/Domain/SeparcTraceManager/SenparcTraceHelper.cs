/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SenparcTraceHelper.cs
    文件功能描述：集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验


    创建标识：Senparc - 20241028

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.SenparcTraceManager
{
    /// <summary>
    /// 单日日志轻量扫描结果（仅计数，不含正文）。
    /// </summary>
    public class SenparcTraceLogScanResult
    {
        public int TotalLogCount { get; set; }
        public int ExceptionLogCount { get; set; }
        public int NormalLogCount => TotalLogCount - ExceptionLogCount;
        public Dictionary<SenparcTraceType, int> TypeCounts { get; set; } = new Dictionary<SenparcTraceType, int>();
    }

    public static class SenparcTraceHelper
    {
        public static string DefaultLogPath { get; set; } = Path.Combine(Senparc.CO2NET.Config.RootDirectoryPath, "App_Data", "SenparcTraceLog");

        private static readonly Regex ExceptionStartRegex = new Regex(@"(?<=\[{3})(\S+)(?=Exception(\]{3}))", RegexOptions.Compiled);
        private static readonly Regex SegmentStartRegex = new Regex(@"(?<=\[{3})([^\]\n\r]+)(?=\]{3})", RegexOptions.Compiled);
        private static readonly Regex ThreadRegex = new Regex(@"(?<=\[{1}线程：)(\d+)(?=\]{1})", RegexOptions.Compiled);
        private static readonly Regex TimeRegex = new Regex(@"(?<=\[{1})([\s\S]{8,30})(?=\]{1})", RegexOptions.Compiled);

        private const string DayStatCacheKeyPrefix = "Admin:SenparcTrace:Scan:";

        /// <summary>
        /// 获取所有日期列表
        /// </summary>
        public static List<string> GetLogDate()
        {
            if (!Directory.Exists(DefaultLogPath))
            {
                return new List<string>();
            }

            var files = Directory.GetFiles(DefaultLogPath, "SenparcTrace-*.log");
            return files
                .Select(z => Path.GetFileNameWithoutExtension(z).Replace("SenparcTrace-", ""))
                .Where(z => !string.IsNullOrEmpty(z))
                .OrderByDescending(z => z)
                .ToList();
        }

        /// <summary>
        /// 轻量扫描指定日期日志：只统计条数/类型，不构建明细对象、不保留正文。
        /// </summary>
        public static async Task<SenparcTraceLogScanResult> GetLogScanResultAsync(
            IServiceProvider serviceProvider,
            string date,
            bool useCache = true)
        {
            var today = SystemTime.Now.ToString("yyyyMMdd");
            var cacheKey = DayStatCacheKeyPrefix + date;
            var cache = serviceProvider?.GetService<IBaseObjectCacheStrategy>();

            if (useCache && cache != null)
            {
                var cached = await cache.GetAsync<SenparcTraceLogScanResult>(cacheKey);
                if (cached != null)
                {
                    return cached;
                }
            }

            var logFile = GetLogFilePath(date);
            if (!File.Exists(logFile))
            {
                throw new Exception("微信日志文件不存在：" + logFile);
            }

            var result = await ScanLogFileAsync(logFile);

            if (useCache && cache != null)
            {
                // 历史日几乎不再增长；当日短缓存，兼顾首页并发与数据新鲜度
                var ttl = string.Equals(date, today, StringComparison.Ordinal)
                    ? TimeSpan.FromSeconds(30)
                    : TimeSpan.FromHours(6);
                await cache.SetAsync(cacheKey, result, ttl);
            }

            return result;
        }

        /// <summary>
        /// 获取指定日期的日志明细（详情页使用；首页统计请用 <see cref="GetLogScanResultAsync"/>）。
        /// </summary>
        public static async Task<List<SenparcTraceItem>> GetAllLogsAsync(IServiceProvider serviceProvider, string date)
        {
            var logFile = GetLogFilePath(date);

            if (!File.Exists(logFile))
            {
                throw new Exception("微信日志文件不存在：" + logFile);
            }

            var logList = new List<SenparcTraceItem>();
            var cache = serviceProvider.GetService<IBaseObjectCacheStrategy>();

            using (var cacheLock = await cache.BeginCacheLockAsync("GetAllLogsAsync", logFile, 100, TimeSpan.FromMilliseconds(100)))
            {
                await using (var readerOwner = await OpenLogReaderAsync(logFile))
                {
                    var sr = readerOwner.Reader;
                    string lineText = null;
                    int line = 0;
                    var readPostData = false;
                    var readResult = false;
                    var readExceptionStackTrace = false;

                    SenparcTraceItem log = new SenparcTraceItem();
                    while ((lineText = await sr.ReadLineAsync()) != null)
                    {
                        line++;
                        lineText = NormalizeLine(lineText);

                        var startExceptionRegex = ExceptionStartRegex.Match(lineText);
                        if (startExceptionRegex.Success)
                        {
                            log = new SenparcTraceItem();
                            logList.Add(log);
                            log.Title = "【{0}Exception】异常！".FormatWith(startExceptionRegex.Value);
                            log.Line = line;
                            log.IsException = true;
                            log.SenparcTraceType = SenparcTraceType.Exception;

                            readPostData = false;
                            readResult = false;
                            readExceptionStackTrace = false;
                            continue;
                        }

                        var startRegex = SegmentStartRegex.Match(lineText);
                        if (startRegex.Success)
                        {
                            log = new SenparcTraceItem();
                            logList.Add(log);
                            log.Title = startRegex.Value;
                            log.Line = line;

                            readPostData = false;
                            readResult = false;
                            readExceptionStackTrace = false;
                            continue;
                        }

                        var threadRegex = ThreadRegex.Match(lineText);
                        if (threadRegex.Success)
                        {
                            log.ThreadId = int.Parse(threadRegex.Value);
                            continue;
                        }

                        var timeRegex = TimeRegex.Match(lineText);
                        if (timeRegex.Success && string.IsNullOrEmpty(log.DateTime))
                        {
                            log.DateTime = timeRegex.Value;
                            continue;
                        }

                        log.Result.TotalResult += lineText + Environment.NewLine;

                        if (readPostData)
                        {
                            log.Result.PostData += lineText + Environment.NewLine;
                            continue;
                        }

                        if (lineText.StartsWith("URL："))
                        {
                            log.Result.Url = lineText.Replace("URL：", "");

                            if (SenparcTraceType.Normal == log.SenparcTraceType)
                            {
                                log.SenparcTraceType = SenparcTraceType.API;
                            }
                        }
                        else if (lineText == "Post Data：")
                        {
                            log.SenparcTraceType = SenparcTraceType.PostRequest;
                            readPostData = true;
                        }
                        else if (lineText == "Result：" || readResult)
                        {
                            log.Result.Result += lineText.Replace("Result：", "") + "\r\n";
                            readResult = true;

                            if (SenparcTraceType.PostRequest != log.SenparcTraceType)
                            {
                                log.SenparcTraceType = SenparcTraceType.GetRequest;
                            }
                        }

                        if (log.IsException)
                        {
                            if (lineText.StartsWith("AccessTokenOrAppId："))
                            {
                                log.Result.ExceptionAccessTokenOrAppId = lineText.Replace("AccessTokenOrAppId：", "");
                            }
                            else if (lineText.StartsWith("Message：") || lineText.StartsWith("errcode："))
                            {
                                log.Result.ExceptionMessage = lineText.Replace("Message：", "");
                            }
                            else if (lineText.StartsWith("StackTrace："))
                            {
                                log.Result.ExceptionStackTrace = lineText.Replace("StackTrace：", "");
                                readExceptionStackTrace = true;
                            }
                            else if (readExceptionStackTrace)
                            {
                                log.Result.ExceptionStackTrace = "\r\n" + lineText;
                            }
                        }
                    }
                }
            }

            logList.Reverse();
            return logList;
        }

        private static string GetLogFilePath(string date)
        {
            return Path.Combine(DefaultLogPath, string.Format("SenparcTrace-{0}.log", date));
        }

        private static string NormalizeLine(string lineText)
        {
            return lineText == null ? string.Empty : lineText.Trim().TrimStart('\uFEFF');
        }

        /// <summary>
        /// 流式扫描日志文件，只维护当前条目类型与计数器。
        /// </summary>
        private static async Task<SenparcTraceLogScanResult> ScanLogFileAsync(string logFile)
        {
            var result = new SenparcTraceLogScanResult();
            var hasCurrent = false;
            var currentType = SenparcTraceType.Normal;
            var readPostData = false;
            var readResult = false;

            void CommitCurrent()
            {
                if (!hasCurrent)
                {
                    return;
                }

                result.TotalLogCount++;
                if (currentType == SenparcTraceType.Exception)
                {
                    result.ExceptionLogCount++;
                }

                if (!result.TypeCounts.ContainsKey(currentType))
                {
                    result.TypeCounts[currentType] = 0;
                }
                result.TypeCounts[currentType]++;
                hasCurrent = false;
            }

            await using (var readerOwner = await OpenLogReaderAsync(logFile))
            {
                var sr = readerOwner.Reader;
                string lineText;
                while ((lineText = await sr.ReadLineAsync()) != null)
                {
                    lineText = NormalizeLine(lineText);
                    if (lineText.Length == 0)
                    {
                        continue;
                    }

                    var exceptionMatch = ExceptionStartRegex.Match(lineText);
                    if (exceptionMatch.Success)
                    {
                        CommitCurrent();
                        hasCurrent = true;
                        currentType = SenparcTraceType.Exception;
                        readPostData = false;
                        readResult = false;
                        continue;
                    }

                    var startMatch = SegmentStartRegex.Match(lineText);
                    if (startMatch.Success)
                    {
                        CommitCurrent();
                        hasCurrent = true;
                        currentType = SenparcTraceType.Normal;
                        readPostData = false;
                        readResult = false;
                        continue;
                    }

                    if (!hasCurrent)
                    {
                        continue;
                    }

                    if (readPostData)
                    {
                        continue;
                    }

                    if (lineText.StartsWith("URL："))
                    {
                        if (currentType == SenparcTraceType.Normal)
                        {
                            currentType = SenparcTraceType.API;
                        }
                    }
                    else if (lineText == "Post Data：")
                    {
                        currentType = SenparcTraceType.PostRequest;
                        readPostData = true;
                    }
                    else if (lineText == "Result：" || readResult)
                    {
                        readResult = true;
                        if (currentType != SenparcTraceType.PostRequest)
                        {
                            currentType = SenparcTraceType.GetRequest;
                        }
                    }
                }
            }

            CommitCurrent();
            return result;
        }

        /// <summary>
        /// 以 FileShare.ReadWrite 直接读正在写入的日志；失败时回退到 .bak 快照。
        /// </summary>
        private static async Task<LogReaderOwner> OpenLogReaderAsync(string logFile)
        {
            try
            {
                var stream = new FileStream(
                    logFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 64 * 1024,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan);

                return new LogReaderOwner(new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true), bakFilePath: null);
            }
            catch (IOException)
            {
                // 写入方未允许共享读时，回退到原 Copy 方案，避免首页/详情失败
                var bakFilename = logFile + ".bak";
                await Task.Run(() =>
                {
                    if (File.Exists(bakFilename))
                    {
                        File.Delete(bakFilename);
                    }
                    File.Copy(logFile, bakFilename, true);
                });

                var bakStream = new FileStream(
                    bakFilename,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 64 * 1024,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);

                return new LogReaderOwner(new StreamReader(bakStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true), bakFilename);
            }
        }

        private sealed class LogReaderOwner : IAsyncDisposable, IDisposable
        {
            private readonly string _bakFilePath;
            private bool _disposed;

            public StreamReader Reader { get; }

            public LogReaderOwner(StreamReader reader, string bakFilePath)
            {
                Reader = reader;
                _bakFilePath = bakFilePath;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Reader.Dispose();
                TryDeleteBak();
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            private void TryDeleteBak()
            {
                if (string.IsNullOrEmpty(_bakFilePath))
                {
                    return;
                }

                try
                {
                    // DeleteOnClose 通常已清理；再兜底一次
                    if (File.Exists(_bakFilePath))
                    {
                        File.Delete(_bakFilePath);
                    }
                }
                catch
                {
                    // 忽略 bak 清理失败，避免影响主流程
                }
            }
        }
    }
}
