using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Log;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service
{
    public class SystemConfigServiceBase : ClientServiceBase<SystemConfig>
    {
        public SystemConfigServiceBase(IClientRepositoryBase<SystemConfig> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        public override void SaveObject(SystemConfig obj)
        {
            LogUtility.SystemLogger.Info("系统信息被编辑");

            base.SaveObject(obj);

            //删除缓存
            var systemConfigCache = _serviceProvider.GetService<FullSystemConfigCache>();
            systemConfigCache.RemoveCache();
        }

        public override async Task SaveObjectAsync(SystemConfig obj)
        {
            LogUtility.SystemLogger.Info("系统信息被编辑");

            await base.SaveObjectAsync(obj);

            //删除缓存
            var systemConfigCache = _serviceProvider.GetService<FullSystemConfigCache>();
            await systemConfigCache.RemoveCacheAsync();
            await base.SaveObjectAsync(obj);
        }

        public virtual string BackupDatabase()
        {
            string timeStamp = DateTime.Now.ToString("yyyyMMdd-HH-mm");//分钟
            return timeStamp;
        }

        public virtual void RecycleAppPool()
        {
            //string webConfigPath = HttpContext.Current.Server.MapPath("~/Web.config");
            //System.IO.File.SetLastWriteTimeUtc(webConfigPath, DateTime.UtcNow);
        }

        public override void DeleteObject(SystemConfig obj)
        {
            throw new Exception("系统信息不能被删除！");
        }
    }
}
