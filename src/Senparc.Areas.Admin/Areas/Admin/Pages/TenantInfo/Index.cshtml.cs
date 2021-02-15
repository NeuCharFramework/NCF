using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.Validator;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Service;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class TenantInfo_IndexModel : BaseAdminPageModel
    {
        private readonly TenantInfoService _tenantInfoService;
        private readonly FullSystemConfigCache _fullSystemConfigCache;

        //[BindProperty]
        //private FullSystemConfig FullSystemConfig { get; set; }


        public TenantInfo_IndexModel(TenantInfoService tenantInfoService, FullSystemConfigCache fullSystemConfigCache)
        {
            this._tenantInfoService = tenantInfoService;
            this._fullSystemConfigCache = fullSystemConfigCache;

            //FullSystemConfig = _fullSystemConfigCache.Data;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adminUserInfoName"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        //[Ncf.AreaBase.Admin.Filters.CustomerResource("admin-get-systemconfig")]
        public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        {
            var tenantInfo = await _tenantInfoService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, OrderingType.Ascending);
            return Ok(new { List = tenantInfo.AsEnumerable() });
        }

        /// <summary>
        /// Handler=Save
        /// ±£´æ
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [CustomerResource("admin-add", "admin-edit")]
        public IActionResult OnPostSave([FromBody]CreateOrUpdate_TenantInfoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return Ok(false);
            }
            bool isExists = this._tenantInfoService.CheckUserNameExisted(dto.Id, dto.UserName);
            if (isExists)
            {
                return Ok(false);
            }
            if (dto.Id > 0)
            {
                //dto.Id = Id;
                _tenantInfoService.UpdateAdminUserInfo(dto);
            }
            else
            {
                _tenantInfoService.CreateAdminUserInfo(dto);
            }
            return Ok(true);
        }
    }
}
