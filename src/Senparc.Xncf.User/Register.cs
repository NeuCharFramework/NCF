using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.User.Functions;
using Senparc.Xncf.User.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Xncf.User.Services;
using Senparc.Xncf.User.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.User
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.User";

        public override string Uid => "CEDD99B6-7114-44CA-B682-400C07F130CB";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "1.0-beta1";//必须填写版本号

        public override string MenuName => "用户管理";

        public override string Icon => "fa fa-star";

        public override string Description => "用户管理模块";

        public override IList<Type> Functions => new Type[] { typeof(MyFunction) };



        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //安装或升级版本时更新数据库
            await base.MigrateDatabaseAsync(serviceProvider);

            //根据安装或更新不同条件执行逻辑
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //新安装
            #region 初始化数据库数据
                    var colorService = serviceProvider.GetService<ColorService>();
                    var color = colorService.GetObject(z => true);
                    if (color == null)//如果是纯第一次安装，理论上不会有残留数据
                    {
                        ColorDto colorDto = await colorService.CreateNewColor().ConfigureAwait(false);//创建默认颜色
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

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            UserSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as UserSenparcEntities;

            //指定需要删除的数据实体

            //注意：这里作为演示，在卸载模块的时候删除了所有本模块创建的表，实际操作过程中，请谨慎操作，并且按照删除顺序对实体进行排序！
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion
    }
}
