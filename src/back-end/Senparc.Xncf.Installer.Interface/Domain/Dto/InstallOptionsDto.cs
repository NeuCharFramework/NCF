using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Installer.Interface.Domain.Dto
{
    public class InstallOptionsDto
    {
        public string SystemName { get; set; }
        public string AdminUserName { get; set; }
        public string DbConnectionString { get; set; }
    }
}
