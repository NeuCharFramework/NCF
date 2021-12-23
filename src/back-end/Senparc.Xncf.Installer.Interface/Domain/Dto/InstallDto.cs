using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Installer.Domain.Dto
{
    public class InstallDto
    {
        public int StatCode { get; set; }
        public string AdminUserName { get; set; }
        public string AdminPassword { get; set; }
        public int Step { get; set; }
        public TenantInfoDto TenantInfoDto { get; set; }
    }
}
