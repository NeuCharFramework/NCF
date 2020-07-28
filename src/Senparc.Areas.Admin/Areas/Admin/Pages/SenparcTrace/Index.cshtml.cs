using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Areas.Admin.SenparcTraceManager;
using Senparc.Ncf.AreaBase.Admin.Filters;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [IgnoreAuth]
    public class SenparcTrace_IndexModel : BaseAdminPageModel
    {
        //public List<string> DateList { get; private set; }

        public void OnGet()
        {
            //DateList = SenparcTraceHelper.GetLogDate();
        }

        public IActionResult OnGetList(int pageIndex = 1, int pageSize = 10)
        {
            var dateList = SenparcTraceHelper.GetLogDate();
            return Ok(new { dateList.Count, pageIndex, List = dateList.Skip((pageIndex - 1) * pageSize) });
        }
    }
}
