using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.Log;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Installer.Domain.Dto;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Xncf.Installer.Domain.Services
{
    public class InstallOptionsService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SenparcCoreSetting  _senparcCoreSetting;
        public InstallOptionsService(IServiceProvider serviceProvider, IOptions<SenparcCoreSetting> senparcCoreSetting)
        {
            this._serviceProvider = serviceProvider;
            this._senparcCoreSetting = senparcCoreSetting.Value;

            //初始化Options的默认值
            
        }

        /// <summary>
        /// 获取自动生成的管理员用户名称
        /// </summary>
        /// <returns></returns>
        public string GetDefaultAdminUserName()
        {
            return $"SenparcCoreAdmin{new Random().Next(100).ToString("00")}";
        }

        /// <summary>
        /// 获取默认的系统名称
        /// </summary>
        /// <returns></returns>
        public string GetDefaultSystemName()
        {
            return "NCF - Template Project";
        }

        /// <summary>
        /// 读取配置中目标数据库连接字符串
        /// </summary>
        /// <returns></returns>
        public string GetDbConnectionString()
        {
            //string dbConfigName = SenparcDatabaseConnectionConfigs.GetFullDatabaseName(_senparcCoreSetting.DatabaseName);
            return SenparcDatabaseConnectionConfigs.ClientConnectionString;
        }
        /// <summary>
        /// 读取模块名称
        /// </summary>
        /// <returns></returns>
        public List<XncfRegisterDto> GetModules()
        {
            var newXncfRegisters = XncfRegisterManager.RegisterList.ToList();
            var needXncfRegisters = new List<XncfRegisterDto>();
            foreach (var item in newXncfRegisters)
            {
                var needXncfRegister = new XncfRegisterDto()
                {
                    IgnoreInstall = item.IgnoreInstall,
                    Uid = item.Uid,
                    Icon = item.Icon,
                    MenuName = item.MenuName,
                    Name = item.Name,
                    Version = item.Version,
                    Description = item.Description
                };
                needXncfRegisters.Add(needXncfRegister);
            }
            return needXncfRegisters;
        }

        /// <summary>
        /// 修改配置及缓存中目标数据库连接字符串
        /// </summary>
        public void ResetDbConnectionString(string dbConnectionString)
        {
            if (dbConnectionString == GetDbConnectionString())
            {
                return;
            }
                
            string dbConfigName = SenparcDatabaseConnectionConfigs.GetFullDatabaseName(_senparcCoreSetting.DatabaseName);

            //清空数据库配置缓存
            MethodCache.ClearMethodCache<ConcurrentDictionary<string, SenparcConfig>>(SenparcDatabaseConnectionConfigs.SENPARC_CONFIG_KEY);

            //修改数据库连接字符串
            Func<ConcurrentDictionary<string, SenparcConfig>> func = () =>
            {
                ConcurrentDictionary<string, SenparcConfig> configs = new ConcurrentDictionary<string, SenparcConfig>();
                try
                {
                    XmlDataContext xmlCtx = new XmlDataContext(SiteConfig.SenparcConfigDirctory);
                    var list = xmlCtx.GetXmlList<SenparcConfig>();
                    list.ForEach(z => configs[z.Name] = z);
                    foreach (var item in list)
                    {
                        if (item.Name == dbConfigName)
                        {
                            item.ConnectionStringFull = dbConnectionString;
                            configs[dbConfigName].ConnectionStringFull = dbConnectionString;
                            break;
                        }
                    }
                    xmlCtx.Save<SenparcConfig>(list);
                }
                catch (Exception e)
                {
                    Console.WriteLine("=== NCF === 修改数据库配置错误：" + e.ToString());
                    LogUtility.WebLogger.ErrorFormat("SenparcConfigs.Configs 修改错误：" + e.Message, e);
                }
                return configs;
            };

            //刷新数据库配置缓存
            _ = MethodCache.GetMethodCache(SenparcDatabaseConnectionConfigs.SENPARC_CONFIG_KEY, func, 60 * 999);
        }
    }
}
