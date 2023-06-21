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
    [IgnoreAntiforgeryToken]
    public class AdminUserInfo_IndexModel : BaseAdminPageModel
    {
        private readonly AdminUserInfoService _adminUserInfoService;
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

        public AdminUserInfo_IndexModel(AdminUserInfoService adminUserInfoService)
        {
            _adminUserInfoService = adminUserInfoService;
        }

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