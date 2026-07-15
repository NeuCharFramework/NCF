/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AccountDto.cs
    文件功能描述：AccountDto 相关功能实现
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260705
    修改描述：v0.0.3 新增登录超时配置并补齐多数据库迁移支持

    修改标识：Senparc - 20260705
    修改描述：v0.0.4 新增登录超时配置并补齐多数据库迁移支持
----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Models.Dto
{
    public class AccountDto
    {
    }

    public class AccountLoginDto
    {
        [Required]
        [MaxLength(55)]
        public string UserName { get; set; }

        [MaxLength(55)]
        [Required]
        public string Password { get; set; }

        [MaxLength(100)]
        public string TenantKey { get; set; }

        /// <summary>
        /// 验证码
        /// </summary>
        public string ValidateCode { get; set; }
    }

    public class AccountLoginResultDto
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Jwt token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// JWT 过期时间（UTC）
        /// </summary>
        public DateTimeOffset? TokenExpiresUtc { get; set; }

        /// <summary>
        /// 角色列表
        /// </summary>
        public IEnumerable<string> RoleCodes { get; set; }

        public IEnumerable<Ncf.Core.Models.DataBaseModel.SysMenuTreeItemDto> MenuTree { get; set; }

        /// <summary>
        /// 操作代码
        /// </summary>
        public IEnumerable<string> PermissionCodes { get; set; }
        public string RealName { get; internal set; }
    }
}
