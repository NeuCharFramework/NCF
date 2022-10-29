using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Xncf.AuditLog.Domain.Services;
using Senparc.Xncf.AuditLog.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Models;
using System.Linq;

namespace Senparc.Xncf.AuditLog.Areas.AuditLog.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly AuditLogService _auditLogService;

        public Index(AuditLogService auditLogService, Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
            this._auditLogService = auditLogService;
        }


        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        {
            var auditLog = await _auditLogService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, OrderingType.Descending);
            return Ok(new { List = auditLog.AsEnumerable() });
        }
        //public void OnGet()
        //{
        //}
    }
}
