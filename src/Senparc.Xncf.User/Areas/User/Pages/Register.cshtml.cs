using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Service;

namespace Senparc.Xncf.User.Areas.User.Pages
{
    [AllowAnonymous]
    public class RegisterModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {

        public MultipleDatabaseType MultipleDatabaseType { get; set; }
        private readonly AdminUserInfoService _userInfoService;
        private readonly IServiceProvider _serviceProvider;
        public RegisterModel(AdminUserInfoService userInfoService, IServiceProvider serviceProvider, Lazy<XncfModuleService> xncfModuleService)
            : base(xncfModuleService)
        {
            _serviceProvider = serviceProvider;
            this._userInfoService = userInfoService;
            var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
            var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
            MultipleDatabaseType = currentDatabaseConfiguration.MultipleDatabaseType;
        }
        /// <summary>
        /// d
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostRegisterAsync([FromBody] CreateOrUpdate_AdminUserInfoDto req)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new { req.UserName, req.Password });
            }

            var userInfo = await _userInfoService.GetUserInfo(req.UserName);
            if (userInfo == null)
            {
                _userInfoService.CreateAdminUserInfo(req);
            }
            var adminUserInfo = _userInfoService.TryLogin(req.UserName, req.Password, true);
            if (adminUserInfo == null)
            {
                return Ok("pwd", false, "×¢²áÊ§°Ü£¡´íÎó´úÂë£º102¡£");
            }
            else
            {
                req.Id = adminUserInfo.Id;
                _userInfoService.UpdateAdminUserInfo(req);
                return Ok(true);
            }
        }
    }
}
