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
    public class SenparcTrace_DateLogModel : BaseAdminPageModel
    {
        public string Date { get; private set; }
        public List<SenparcTraceItem> WeixinTraceItemList { get; private set; }

        public async Task OnGetAsync(string date)
        {
            await Task.CompletedTask;
            Date = date;
            WeixinTraceItemList = SenparcTraceHelper.GetAllLogs(date);
        }

        public async Task<IActionResult> OnGetDetailAsync(string date)
        {
            await Task.CompletedTask;
            var weixinTraceItemList = SenparcTraceHelper.GetAllLogs(date);
            return Ok(weixinTraceItemList);
        }
    }
}
