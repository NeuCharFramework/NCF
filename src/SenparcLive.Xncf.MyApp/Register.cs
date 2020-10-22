using System;
using System.Collections.Generic;
using Senparc.Ncf.XncfBase;
using SenparcLive.Xncf.MyApp.Functions;
using SenparcLive.Xncf.MyApp.Models.DatabaseModel;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using SenparcLive.Xncf.MyApp.Services;
using SenparcLive.Xncf.MyApp.Models.DatabaseModel.Dto;

namespace SenparcLive.Xncf.MyApp
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IRegister 接口

        public override string Name => "SenparcLive.Xncf.MyApp";

        public override string Uid => "E13D7B0B-BC93-48FF-B844-2C11FF4161D5";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.2";//必须填写版本号

        public override string MenuName => "直播创建模块";

        public override string Icon => "fa fa-star";

        public override string Description => "大家好！";

        public override IList<Type> Functions => new Type[] { typeof(MyFunction) /*typeof(BuildXncf)*/ };


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
            MyAppSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as MyAppSenparcEntities;

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
