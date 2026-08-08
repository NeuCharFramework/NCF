/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminUserInfo_Request.cs
    文件功能描述：后台管理员用户请求模型
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强后台模块批量更新并完善多语言管理界面

----------------------------------------------------------------*/
using Senparc.Ncf.XncfBase.FunctionRenders;
using Microsoft.Extensions.Localization;
using Senparc.Areas.Admin;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.PL
{
    public class AdminUserInfo_LoginRequest : FunctionAppRequestBase
    {
        [Required]
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class AdminUserInfo_AddRoleRequest
    {
        /// <summary>
        /// 角色Id
        /// </summary>
        [Required]
        public IEnumerable<string> RoleId { get; set; }

        /// <summary>
        /// 管理员id
        /// </summary>
        public int AccountId { get; set; }
    }

    /// <summary>
    /// 管理员新增
    /// </summary>
    public class AdminUserInfo_CreateOrUpdateRequest : IValidatableObject
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        [MaxLength(35)]
        [MinLength(5)]
        public string UserName
        {
            get;
            set;
        }

        [MaxLength(30)]
        [MinLength(6)]
        public string Password
        {
            get;
            set;
        }

        [MaxLength(555)]
        public string Note
        {
            get;
            set;
        }

        [MaxLength(35)]
        public string RealName
        {
            get;
            set;
        }

        [MaxLength(25)]
        public string Phone
        {
            get;
            set;
        }

        /// <summary>
        /// 备注1
        /// </summary>
        [MaxLength(150)]
        public string AdminRemark
        {
            get;
            set;
        }

        /// <summary>
        /// 备注2
        /// </summary>
        [MaxLength(150)]
        public string Remark
        {
            get;
            set;
        }

        public int Id { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Password) && Id == 0)
            {
                var localizer = validationContext.GetService(typeof(IStringLocalizer<AdminResource>)) as IStringLocalizer<AdminResource>;
                yield return new ValidationResult(localizer?["AdminUserInfo.PasswordRequired"].Value ?? "密码为必填项！", new[] { "Password" });
            }
        }
    }
}
