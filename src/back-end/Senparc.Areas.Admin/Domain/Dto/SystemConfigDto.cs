using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Dto
{
    public class SystemConfigDto
    {
        public int Id { get; set; }
        public string SystemName { get; private set; }

        public string MchId { get; private set; }

        public string MchKey { get; private set; }

        public string TenPayAppId { get; private set; }

        public bool? HideModuleManager { get; private set; }
    }

    public class SystemConfig_CreateOrUpdateDto
    {
        public int Id { get; set; }
        public string SystemName { get; private set; }

        public string MchId { get; private set; }

        public string MchKey { get; private set; }

        public string TenPayAppId { get; private set; }

        public bool? HideModuleManager { get; private set; }
    }
}
