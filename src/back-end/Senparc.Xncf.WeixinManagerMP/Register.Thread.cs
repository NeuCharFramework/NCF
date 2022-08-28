using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase.Threads;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Xncf.WeixinManagerMP.Domain.Services;

namespace Senparc.Xncf.WeixinManagerMP
{
    public partial class Register : IXncfThread
    {
        public void ThreadConfig(XncfThreadBuilder xncfThreadBuilder)
        {
            xncfThreadBuilder.AddThreadInfo(new Ncf.XncfBase.Threads.ThreadInfo(
                name: "定时公众号用户同步",
                intervalTime: TimeSpan.FromSeconds(30),
                task: async (app, threadInfo) =>
                {
                    try
                    {
                        //SenparcTrace.SendCustomLog("执行调试", "DatabaseToolkit.Register.ThreadConfig");
                        threadInfo.RecordStory("开始同步信息");

                        using (var scope = app.ApplicationServices.CreateScope())
                        {
                            var serviceProvider = scope.ServiceProvider;

                            //检测当前模块是否可用
                            XncfRegisterManager xncfRegisterManager = new XncfRegisterManager(serviceProvider);
                            var xncfIsValiable = await xncfRegisterManager.CheckXncfValiable(this);

                            if (!xncfIsValiable)
                            {
                                throw new NcfModuleException($"{this.MenuName} 模块当前不可用，跳过自动更新");
                            }

                            var mpUserService = serviceProvider.GetRequiredService<MpUserService>();

                            var dt = SystemTime.Now;
                            var changedCount = await mpUserService.SyncMpUser();
                            threadInfo.RecordStory($"总共更新了 {changedCount} 条数据，总耗时：{SystemTime.DiffTotalMS(dt)} 毫秒");
                        }
                    }
                    catch (NcfModuleException ex)
                    {
                        throw;
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        threadInfo.RecordStory("检测并备份结束");
                    }
                },
                exceptionHandler: ex =>
                {
                    SenparcTrace.SendCustomLog("DatabaseToolkit", $@"{ex.Message}
{ex.StackTrace}
{ex.InnerException?.StackTrace}");
                    return Task.CompletedTask;
                }));
        }
    }
}
