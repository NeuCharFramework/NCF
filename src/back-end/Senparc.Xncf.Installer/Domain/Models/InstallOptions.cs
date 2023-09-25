using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Installer.Domain
{
    public class InstallOptions
    {
        public string SystemName { get; set; }
        public string AdminUserName { get; set; }
        public string DbConnectionString { get; set; }

        public InstallOptions()
        {
            SystemName = "NCF - Template Project";
            AdminUserName = GenerateUserName();
            DbConnectionString = "";
        }

        private string GenerateUserName()
        {
            return $"SenparcCoreAdmin{new Random().Next(100).ToString("00")}";
        }
    }
}
