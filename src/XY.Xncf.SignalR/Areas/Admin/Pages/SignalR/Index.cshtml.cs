using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Service;
using XY.Xncf.SignalR.Hubs;

namespace XY.Xncf.SignalR.Areas.Admin.Pages.SignalR
{
    public class IndexModel : AdminXncfModulePageModelBase
    {
        public IndexModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
        }

        public void OnGet()
        {
        }

        public IActionResult OnGetAjax(int pageIndex = 1, int pageSize = 100, string userName = null)
        {
            var allList = GlobleConnectionManage.ClientList
               .Where(w => userName == null || userName == "" || (w.Value != null && w.Value.Any(c => c.UserName != null && c.UserName.Contains(userName))));
            List<KeyValuePair<string, List<Client>>> _list = allList
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            List<KeyValuePair<string, int>> userList = new List<KeyValuePair<string, int>>();
            foreach (var user in _list)
            {
                var _userName = string.IsNullOrWhiteSpace(user.Value.First().UserName) ? ("ÄäÃû" + user.Key.Substring(0, 4)) : user.Value.First().UserName;
                userList.Add(new KeyValuePair<string, int>(_userName, user.Value.Count));
            }
            return Ok(new { total = allList.Count(), list = userList });
        }
    }
}
