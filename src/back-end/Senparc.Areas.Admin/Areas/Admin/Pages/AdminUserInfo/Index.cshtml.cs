/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Index.cshtml.cs
    文件功能描述：Index.cshtml.cs 相关实现
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [AutoValidateAntiforgeryToken]
    public class AdminUserInfo_IndexModel(IServiceProvider serviceProvider, AdminUserInfoService adminUserInfoService) 
        : BaseAdminPageModel(serviceProvider)
    {
        private readonly AdminUserInfoService _adminUserInfoService = adminUserInfoService;
        public PagedList<AdminUserInfo> AdminUserInfoList { get; set; }

        /// <summary>
        /// 属性绑定，支持GET
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 排序方法
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string OrderField { get; set; } = "AddTime Desc,Id";

        public async Task<IActionResult> OnGetAsync()
        {
            await Task.CompletedTask;
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adminUserInfoName"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("admin-search")]
        public async Task<IActionResult> OnGetListAsync(string adminUserInfoName, int pageIndex, int pageSize)
        {
            var seh = new SenparcExpressionHelper<AdminUserInfo>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(adminUserInfoName), _ => _.UserName.Contains(adminUserInfoName));
            var where = seh.BuildWhereExpression();
            var admins = await _adminUserInfoService.GetObjectListAsync(pageIndex, pageSize, where, OrderField);
            return Ok(new { admins.TotalCount, admins.PageIndex, List = admins.AsEnumerable() });
        }

        /// <summary>
        /// Handler=Delete
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("admin-delete")]
        public IActionResult OnPostDelete([FromBody]int[] ids)
        {
            foreach (var id in ids)
            {
                _adminUserInfoService.DeleteObject(id);
            }
            return Ok(ids.Length);
            //return RedirectToPage("./Index");
        }
    }
}