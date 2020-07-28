using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Utility.StreamExtensions;
using System.Net.Http;

namespace Senparc.Mvc.CustomActionResults
{
    /// <summary>
    /// 压缩文件，参考：https://blog.stephencleary.com/2016/11/streaming-zip-on-aspnet-core.html
    /// </summary>
    public class ZipResult : FileResult
    {
        //参考：https://github.com/StephenClearyExamples/AsyncDynamicZip/issues/1
        private static HttpClient Client { get; } = new HttpClient();

        private Func<Stream, ActionContext, Task> _callback;

        public ZipResult(Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType, string zipFileName,
            IEnumerable<string> filenames,
             //Dictionary<string, string> files,
            Dictionary<string, string> newFilesFromString,
            Func<Stream, ActionContext, Task> callback)
            : base((contentType ?? new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream")).ToString())
        {
            if (callback == null)
            {
                callback = async (outputStream, _) =>
                {
                    using (var zipArchive = new ZipArchive(new WriteOnlyStreamWrapper(outputStream), ZipArchiveMode.Create))
                    {
                        Dictionary<string, string> files = new Dictionary<string, string>();
                        foreach (var item in filenames)
                        {
                            files[Path.GetFileName(item)] = item;
                        }
                        foreach (var item in newFilesFromString)
                        {
                            //COCONET TODO:完善字符串的插入
                        }

                        foreach (var kvp in files)
                        {
                            var zipEntry = zipArchive.CreateEntry(kvp.Key);
                            using (var zipStream = zipEntry.Open())
                            {
                                //COCONET 需要测试。原始方法在.net core 2.1中找不到：Microsoft.AspNetCore.Sockets.Client.GetStreamAsync() 
                                using (var stream = await Client.GetStreamAsync(kvp.Value))
                                {
                                    await stream.CopyToAsync(zipStream);
                                }
                            }
                        }
                    }
                };
            }
            //throw new ArgumentNullException(nameof(callback));

            _callback = callback;
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));


            var executor = new FileCallbackResultExecutor(context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>());
            return executor.ExecuteAsync(context, this);
        }

        private sealed class FileCallbackResultExecutor : FileResultExecutorBase
        {
            public FileCallbackResultExecutor(ILoggerFactory loggerFactory)
                : base(CreateLogger<FileCallbackResultExecutor>(loggerFactory))
            {
            }

            public Task ExecuteAsync(ActionContext context, ZipResult result)
            {
                SetHeadersAndLog(context, result,
                    //COCONET 为编译通过暂时这么写，应该从传入的Stream中获取
                    context.HttpContext.Response.Body.Length,
                    true);
                return result._callback(context.HttpContext.Response.Body, context);
            }
        }
    }
}
