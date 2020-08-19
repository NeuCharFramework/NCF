using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.ExtensionAreaTemplate.Functions;
using Senparc.Xncf.ExtensionAreaTemplate.Models.DatabaseModel;
using Senparc.Xncf.ExtensionAreaTemplate.Models.DatabaseModel.Dto;
using Senparc.Xncf.ExtensionAreaTemplate.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.ExtensionAreaTemplate
{
    public partial class Register : XncfRegisterBase,
                                    IXncfRegister //注册 XNCF 基础模块接口（必须）
                                    // 更多注册类型见 Register.xx.cs
    {
        public Register()
        { }

        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.ExtensionAreaTemplate";
        public override string Uid => "1052306E-8C78-4EBF-8CA9-0EB3738350AE";//必须确保全局唯一，生成后必须固定
        public override string Version => "0.2.2";//必须填写版本号

        public override string MenuName => "扩展页面测试模块";
        public override string Icon => "fa fa-dot-circle-o";//参考如：https://colorlib.com/polygon/gentelella/icons.html
        public override string Description => "这是一个示例项目，用于展示如何扩展自己的网页、功能模块，学习之后可以删除，也可以以此为基础模板，改成自己的扩展模块（XNCF Modules）。";

        /// <summary>
        /// 注册当前模块需要支持的功能模块
        /// </summary>
        public override IList<Type> Functions => new Type[] {
            typeof(DownloadSourceCode)
        };


        /// <summary>
        /// 安装或更新过程需要执行的业务
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="installOrUpdate"></param>
        /// <returns></returns>
        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //更新数据库
            await base.MigrateDatabaseAsync<MySenparcEntities>(serviceProvider);

            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //新安装
                    var colorService = serviceProvider.GetService<ColorService>();
                    var color = colorService.GetObject(z => true);
                    if (color == null)//如果是纯第一次安装，理论上不会有残留数据
                    {
                        //创建默认颜色
                        ColorDto colorDto = await colorService.CreateNewColor().ConfigureAwait(false);
                    }
                    break;
                case InstallOrUpdate.Update:
                    //更新
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        /// <summary>
        /// 删除模块时需要执行的业务
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="unsinstallFunc"></param>
        /// <returns></returns>
        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            MySenparcEntities mySenparcEntities = serviceProvider.GetService<MySenparcEntities>();

            //指定需要删除的数据实体

            //注意：这里作为演示，删除了所有的表，实际操作过程中，请谨慎操作，并且按照删除顺序对实体进行排序！
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.XncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            await unsinstallFunc().ConfigureAwait(false);
        }

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {
            //任何需要注册的对象
            return base.AddXncfModule(services, configuration);
        }

        #endregion
    }
}
