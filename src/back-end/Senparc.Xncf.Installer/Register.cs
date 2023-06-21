using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Installer.Domain.Dto;
using Senparc.Xncf.Installer.Domain.Services;
using Senparc.Xncf.Installer.OHS.Local.AppService;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.Installer
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.Installer";

        public override string Uid => "62FBB022-B04E-423F-82FE-926D418A0815";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.3";//必须填写版本号

        public override string MenuName => "NCF 安装程序";

        public override string Icon => "fa fa-download";

        public override string Description => "NCF 初始化安装模块，安装完成后可卸载或移除";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddScoped<InstallerService>();
            services.AddScoped<InstallAppService>();
            services.AddScoped<InstallDto>();
            return base.AddXncfModule(services, configuration, env);
        }
    }
}
