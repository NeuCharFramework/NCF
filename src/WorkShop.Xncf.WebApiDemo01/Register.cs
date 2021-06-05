#define RELEASE

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Senparc.Weixin.RegisterServices;
using Microsoft.Extensions.DependencyInjection;
using WorkShop.Xncf.WebApiDemo01.Functions;
using WorkShop.Xncf.WebApiDemo01.Models.DatabaseModel;
namespace WorkShop.Xncf.WebApiDemo01
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "WorkShop.Xncf.WebApiDemo01";

        public override string Uid => "093D1413-8AAA-4E68-AD8E-DBF03E843318";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "1.0";//必须填写版本号

        public override string MenuName => "平台管理";

        public override string Icon => "fa fa-star";

        public override string Description => "平台管理";

        public override IList<Type> Functions => new Type[] { typeof(MyFunction) };

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {

        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }

#if RELEASE
        //生成之前先注释一下代码
        /// <summary>
        /// 模块自定义配置文件
        /// </summary>
        public static IConfiguration FileServerConfiguration;

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {
            FileServerConfiguration = new ConfigurationBuilder().AddJsonFile("fileserverconfig.json", optional: true, reloadOnChange: true).Build();
            services.AddSenparcWeixinServices(configuration);
            return base.AddXncfModule(services, configuration);
        }
#endif

        #endregion
    }
}
