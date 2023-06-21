using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Text;

namespace Senparc.Areas.Admin.Domain.Services
{
    public class SysRoleService : BaseClientService<SysRole>
    {
        public SysRoleService(ClientRepositoryBase<SysRole> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
    }
}
