/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SystemConfigDto.cs
    文件功能描述：SystemConfigDto 相关功能实现
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260705
    修改描述：v0.0.3 新增登录超时配置并补齐多数据库迁移支持

    修改标识：Senparc - 20260705
    修改描述：v0.0.4 新增登录超时配置并补齐多数据库迁移支持
----------------------------------------------------------------*/

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
        public string SystemName { get; set; }

        public string MchId { get; set; }

        public string MchKey { get; set; }

        public string TenPayAppId { get; set; }

        public bool? HideModuleManager { get; set; }
    }

    public class SystemConfig_CreateOrUpdateDto
    {
        public int Id { get; set; }
        public string SystemName { get; set; }

        public string MchId { get; set; }

        public string MchKey { get; set; }

        public string TenPayAppId { get; set; }

        public bool? HideModuleManager { get; set; }
    }
}
