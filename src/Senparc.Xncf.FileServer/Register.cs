using System;
using System.Collections.Generic;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.FileServer.Models.DatabaseModel;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.FileServer.Services;
using Senparc.Xncf.FileServer.Models.DatabaseModel.Dto;
using Microsoft.Extensions.Configuration;
using Senparc.Ncf.Service;
using Senparc.Xncf.FileServer.Utility;

namespace Senparc.Xncf.FileServer
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IRegister 接口

        public override string Name => "Senparc.Xncf.FileServer";

        public override string Uid => "c13eee61-8989-497d-b355-a08db550dd53";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "1.0";//必须填写版本号

        public override string MenuName => "文件服务器";

        public override string Icon => "fa fa-star";

        public override string Description => "文件服务器";

        public override IList<Type> Functions => new Type[] {  /*typeof(BuildXncf)*/ };

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //安装或升级版本时更新数据库
            await base.MigrateDatabaseAsync<FileServerSenparcEntities>(serviceProvider);

            //根据安装或更新不同条件执行逻辑
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //新安装
                    #region 初始化数据库数据
                    var sysKeyService = serviceProvider.GetService<SysKeyService>();
                    sysKeyService.SaveObject(new SysKey()
                    {
                        AppId = UniqueHelper.LongId().ToString(),
                        AppKey = UniqueHelper.LongId().ToString() + UniqueHelper.LongId().ToString(),
                        Name = "默认"
                    });
                    var sysKey = sysKeyService.GetObject(z => true);
                    if (sysKey == null)//如果是纯第一次安装，理论上不会有残留数据
                    {
                    }
                    #endregion
                    break;
                case InstallOrUpdate.Update:
                    //更新
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region 删除数据库（演示）

            FileServerSenparcEntities mySenparcEntities = serviceProvider.GetService<FileServerSenparcEntities>();

            //指定需要删除的数据实体

            ////注意：这里作为演示，在卸载模块的时候删除了所有本模块创建的表，实际操作过程中，请谨慎操作，并且按照删除顺序对实体进行排序！
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.XncfDatabaseDbContextType).Keys.ToArray().Reverse().ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion

            await unsinstallFunc().ConfigureAwait(false);
        }
        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {
            return base.AddXncfModule(services, configuration);
        }
        #endregion
    }
}
