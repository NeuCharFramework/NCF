using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Senparc.Xncf.AgentsManager.Models;
using Senparc.Xncf.AgentsManager.OHS.Local.AppService;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;

namespace Senparc.Xncf.AgentsManager
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.AgentsManager";

        public override string Uid => "D858D7FA-775A-4690-9023-CFB0B3B84994";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.1.3";//必须填写版本号

        public override string MenuName => "Agents 管理模块";

        public override string Icon => "fa fa-star";

        public override string Description => "Agents 管理模块";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //安装或升级版本时更新数据库
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //根据安装或更新不同条件执行逻辑
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //新安装
                    #region 初始化数据库数据
                    //var colorService = serviceProvider.GetService<ColorAppService>();
                    //var colorResult = await colorService.GetOrInitColorAsync();
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
            AgentsManagerSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AgentsManagerSenparcEntities;

            //指定需要删除的数据实体

            //注意：这里作为演示，在卸载模块的时候删除了所有本模块创建的表，实际操作过程中，请谨慎操作，并且按照删除顺序对实体进行排序！
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //AutoMap映射
            base.AddAutoMapMapping(profile =>
            {
                profile.CreateMap<AgentTemplate, AgentTemplateDto>().ReverseMap();
                profile.CreateMap<ChatGroup, ChatGroupDto>().ReverseMap();
                profile.CreateMap<ChatGroupMember, ChatGroupMemberDto>().ReverseMap();
                profile.CreateMap<ChatGroupHistory, ChatGroupHistoryDto>().ReverseMap();
            });


            return base.AddXncfModule(services, configuration, env);
        }
    }
}



