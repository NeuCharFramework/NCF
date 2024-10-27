using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Senparc.Areas.Admin.Domain;
using Senparc.CO2NET;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
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
        private readonly SenparcCoreSetting _senparcCoreSetting;
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
            return SenparcDatabaseConnectionConfigs.GetClientConnectionString();
        }
        /// <summary>
        /// 读取模块名称
        /// </summary>
        /// <returns></returns>
        public List<XncfRegisterDto> GetModules()
        {

            Senparc.Xncf.Tenant.Register tenantRegister = new Senparc.Xncf.Tenant.Register();

            Senparc.Xncf.SystemCore.Register systemCoreRegister = new Senparc.Xncf.SystemCore.Register();

            Senparc.Xncf.SystemManager.Register systemManagerRegister = new Senparc.Xncf.SystemManager.Register();

            Senparc.Xncf.SystemPermission.Register systemPermissionRegister = new Senparc.Xncf.SystemPermission.Register();

            Senparc.Xncf.XncfModuleManager.Register xncfModuleManagerRegister = new Senparc.Xncf.XncfModuleManager.Register();

            Senparc.Xncf.AreasBase.Register areasBaseRegister = new Senparc.Xncf.AreasBase.Register();

            Senparc.Xncf.Installer.Register installerRegister = new Senparc.Xncf.Installer.Register();

            Senparc.Xncf.Menu.Register menuRegister = new Senparc.Xncf.Menu.Register();
            var newXncfRegisters = XncfRegisterManager.RegisterList.Where(s => s.Uid!= tenantRegister.Uid && s.Uid!= systemCoreRegister.Uid && s.Uid!= systemManagerRegister.Uid
            && s.Uid!= systemPermissionRegister.Uid && s.Uid!= xncfModuleManagerRegister.Uid && s.Uid!= areasBaseRegister.Uid && s.Uid!= installerRegister.Uid && s.Uid != menuRegister.Uid).ToList();
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

            try
            {
                XmlDataContext xmlCtx = new XmlDataContext(SiteConfig.SenparcConfigDirctory);
                var list = xmlCtx.GetXmlList<SenparcConfig>();
                var modifyItem = list.FirstOrDefault(z => z.Name == dbConfigName);
                if (modifyItem == null)
                {
                    throw new NcfExceptionBase($"找不到数据库配置：{dbConfigName}");
                }
                modifyItem.ConnectionStringFull = dbConnectionString;

                xmlCtx.Save<SenparcConfig>(list);
            }
            catch (Exception e)
            {
                Console.WriteLine("=== NCF === 修改数据库配置错误：" + e.ToString());
                LogUtility.WebLogger.ErrorFormat("SenparcConfigs.Configs 修改错误：" + e.Message, e);
            }

            //清空数据库配置缓存
            MethodCache.ClearMethodCache<ConcurrentDictionary<string, SenparcConfig>>(SenparcDatabaseConnectionConfigs.SENPARC_CONFIG_KEY);

            _ = SenparcDatabaseConnectionConfigs.Configs.ToJson();
        }
    }
}
