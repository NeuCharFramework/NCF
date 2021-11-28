using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class MenuIndexModel : BaseAdminPageModel
    {
        private readonly SysMenuService _sysMenuService;

        public MenuIndexModel(SysMenuService _sysMenuService)
        {
            CurrentMenu = "Menu";
            this._sysMenuService = _sysMenuService;
        }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 
        /// </summary>
        public PagedList<SysMenu> SysMenus { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task OnGetAsync()
        {
            SysMenus = await _sysMenuService.GetObjectListAsync(PageIndex, 10, _ => true, _ => _.AddTime, Ncf.Core.Enums.OrderingType.Descending);
        }

        public IActionResult OnPostDelete(string[] ids)
        {
            foreach (var id in ids)
            {
                _sysMenuService.DeleteObject(_ => _.Id == id);
            }

            return RedirectToPage("./Index");
        }
    }
}