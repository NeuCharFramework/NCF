using Microsoft.Extensions.DependencyInjection;
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
    public static class SenparcTraceHelper
    {
        public static string DefaultLogPath { get; set; } = Path.Combine(Senparc.CO2NET.Config.RootDirectoryPath, "App_Data", "SenparcTraceLog");// Path.Combine(Senparc.CO2NET.Config.RootDictionaryPath, "App_Data", "SenparcTraceLog");

        /// <summary>
        /// 获取所有日期列表
        /// </summary>
        /// <returns></returns>
        public static List<string> GetLogDate()
        {
            var files = System.IO.Directory.GetFiles(DefaultLogPath, "*.log");
            return files.Select(z => Path.GetFileNameWithoutExtension(z).Replace("SenparcTrace-", "")).OrderByDescending(z => z).ToList();
        }

        /// <summary>
        /// 获取指定日期的日志
        /// </summary>
        /// <returns></returns>
        public static async Task<List<SenparcTraceItem>> GetAllLogsAsync(IServiceProvider serviceProvider, string date)
        {
            var logFile = Path.Combine(DefaultLogPath, string.Format("SenparcTrace-{0}.log", date));

            if (!File.Exists(logFile))
            {
                throw new Exception("微信日志文件不存在：" + logFile);
            }

            var logList = new List<SenparcTraceItem>();
            var cache = serviceProvider.GetService<IBaseObjectCacheStrategy>();

            using (var cacheLock = await cache.BeginCacheLockAsync("GetAllLogsAsync", logFile, 100, TimeSpan.FromMilliseconds(100)))
            {
                string bakFilename = logFile + ".bak";//备份文件名
                System.IO.File.Delete(bakFilename);
                System.IO.File.Copy(logFile, bakFilename, true);//读取备份文件，以免资源占用

                using (StreamReader sr = new StreamReader(bakFilename, Encoding.UTF8))
                {
                    string lineText = null;
                    int line = 0;
                    var readPostData = false;
                    var readResult = false;
                    var readExceptionStackTrace = false;

                    SenparcTraceItem log = new SenparcTraceItem();
                    while ((lineText = await sr.ReadLineAsync()) != null)
                    {
                        line++;

                        lineText = lineText.Trim();


                        var startExceptionRegex = Regex.Match(lineText, @"(?<=\[{3})(\S+)(?=Exception(\]{3}))");

                        if (startExceptionRegex.Success)
                        {
                            //一个片段的开始（异常）
                            log = new SenparcTraceItem();
                            logList.Add(log);
                            log.Title = "【{0}Exception】异常！".FormatWith(startExceptionRegex.Value);//记录标题
                            log.Line = line;
                            log.IsException = true;
                            log.SenparcTraceType = SenparcTraceType.Exception;

                            readPostData = false;
                            readResult = false;
                            readExceptionStackTrace = false;
                            continue;
                        }

                        //其他自定义类型
                        var startRegex = Regex.Match(lineText, @"(?<=\[{3})([^\]\n\r]+)(?=\]{3})");
                        if (startRegex.Success)
                        {
                            //一个片段的开始
                            log = new SenparcTraceItem();
                            logList.Add(log);
                            log.Title = startRegex.Value;//记录标题
                            log.Line = line;

                            readPostData = false;
                            readResult = false;
                            readExceptionStackTrace = false;
                            continue;
                        }


                        var threadRegex = Regex.Match(lineText, @"(?<=\[{1}线程：)(\d+)(?=\]{1})");
                        if (threadRegex.Success)
                        {
                            //线程
                            log.ThreadId = int.Parse(threadRegex.Value);
                            continue;
                        }

                        var timeRegex = Regex.Match(lineText, @"(?<=\[{1})([\s\S]{8,30})(?=\]{1})");
                        if (timeRegex.Success && string.IsNullOrEmpty(log.DateTime))
                        {
                            //时间
                            log.DateTime = timeRegex.Value;
                            continue;
                        }


                        //内容
                        log.Result.TotalResult += lineText + Environment.NewLine;

                        if (readPostData)
                        {
                            log.Result.PostData += lineText + Environment.NewLine;
                            continue;//一直读到底
                        }


                        if (lineText.StartsWith("URL："))
                        {
                            log.Result.Url = lineText.Replace("URL：", "");

                            if (SenparcTraceType.Normal == log.SenparcTraceType)
                            {
                                log.SenparcTraceType = SenparcTraceType.API;
                            }
                            //log.weixinTraceType = log.weixinTraceType | WeixinTraceType.API;
                        }
                        else if (lineText == "Post Data：")
                        {
                            log.SenparcTraceType = SenparcTraceType.PostRequest;//POST请求
                            readPostData = true;
                        }
                        else if (lineText == "Result：" || readResult)
                        {
                            log.Result.Result += lineText.Replace("Result：", "") + "\r\n";
                            readResult = true;

                            if (SenparcTraceType.PostRequest != log.SenparcTraceType)
                            {
                                log.SenparcTraceType = SenparcTraceType.GetRequest;//GET请求
                            }
                        }

                        if (log.IsException)
                        {
                            //异常信息处理
                            if (lineText.StartsWith("AccessTokenOrAppId："))
                            {
                                log.Result.ExceptionAccessTokenOrAppId = lineText.Replace("AccessTokenOrAppId：", "");
                            }
                            else if (lineText.StartsWith("Message：") || lineText.StartsWith("errcode："))
                            {
                                log.Result.ExceptionMessage = lineText.Replace("Message：", "");//“errcode：”保留
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

                System.IO.File.Delete(bakFilename);//删除备份文件
            }

            logList.Reverse();//翻转序列
            return logList;
        }
    }
}
