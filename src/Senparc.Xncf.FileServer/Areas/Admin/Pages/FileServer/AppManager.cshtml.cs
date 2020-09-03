using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Xncf.FileServer.Models.DatabaseModel.Dto;
using Senparc.Xncf.FileServer.Models.VD;
using Senparc.Xncf.FileServer.Services;
using Senparc.Xncf.FileServer.Utility;

namespace Senparc.Xncf.FileServer.Areas.Admin.Pages.FileServer
{
    public class AppManagerModel : AdminXncfModulePageModelBase
    {
        private readonly SysKeyService _sysKeyService;
        public AppManagerModel(Lazy<XncfModuleService> xncfModuleService, SysKeyService sysKeyService) : base(xncfModuleService)
        {
            this._sysKeyService = sysKeyService;
        }
        public async Task OnGetAsync()
        {
        }

        /// <summary>
        /// Handler=Ajax
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetAjaxAsync(int pageIndex = 1, int pageSize = 10)
        {
            var result = await _sysKeyService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);
            var syskeyDtos = new PagedList<SysKeyDto>(result.Select(z => _sysKeyService.Mapper.Map<SysKeyDto>(z)).ToList(), result.PageIndex, result.PageCount, result.TotalCount);
            return Ok(new { syskeyDtos.TotalCount, pageIndex, pageSize, list = syskeyDtos.AsEnumerable() });
        }

        public async Task<IActionResult> OnPostEditAsync([FromBody] SysKeyDto sysKeyDto)
        {
            SysKey sysKey = null;
            if (sysKeyDto.Id > 0)
            {
                sysKey = await _sysKeyService.GetObjectAsync(z => z.Id == sysKeyDto.Id);
                if (sysKey == null) return Ok(false, "App信息不存在！");
                var exit = await _sysKeyService.GetObjectAsync(z => z.Id != sysKey.Id && z.Name == sysKeyDto.Name);
                if (exit != null) return Ok(false, "更名后的App信息已存在！");
                sysKeyDto.AddTime = sysKey.AddTime;
                _sysKeyService.Mapper.Map(sysKeyDto, sysKey);
                sysKey.LastUpdateTime = DateTime.Now;
            }
            else
            {
                var exit = await _sysKeyService.GetObjectAsync(z => z.Name == sysKeyDto.Name);
                if (exit != null) return Ok(false, "App信息已存在，请更名名称后重试！");
                sysKey = new SysKey()
                {
                    AppId = UniqueHelper.LongId().ToString(),
                    AppKey = UniqueHelper.LongId().ToString() + UniqueHelper.LongId().ToString(),
                    Name = sysKeyDto.Name,
                    AdminRemark = sysKeyDto.AdminRemark
                };
            }
            await _sysKeyService.SaveObjectAsync(sysKey);
            return Ok(new { id = sysKey.Id, uid = Uid });
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] int[] ids)
        {
            await _sysKeyService.DeleteObjectAsync(d => ids.Contains(d.Id));
            return Ok(ids.Length);
            //return RedirectToPage("./Index");
        }
    }
}
