using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Senparc.CO2NET.HttpUtility;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.NeuChar.App.Utilities;
using Senparc.Xncf.FileServer.Models.DatabaseModel.Dto;
using Senparc.Xncf.FileServer.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileServer.Areas.FileServer.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly SysKeyService _sysKeyService;
        private readonly FileRecordService _fileRecordService;
        public string Token { get; set; }
        public string UpFileUrl { get; set; }
        public string BaseUrl { get; set; }
        public Index(Lazy<XncfModuleService> xncfModuleService, SysKeyService sysKeyService, FileRecordService fileRecordService) : base(xncfModuleService)
        {
            this._sysKeyService = sysKeyService;
            this._fileRecordService = fileRecordService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var syskeyModel = await _sysKeyService.GetObjectAsync(i => i.Name == "默认");
            if (syskeyModel == null) return RenderError("默认账号不存在！");
            var url = $"{Request.Scheme}://{Request.Host.Value}/v1/appmanage/gettoken?AppId={syskeyModel.AppId}&AppKey={syskeyModel.AppKey}";
            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
            };
            HttpClient httpClient = new HttpClient(httpClientHandler);
            var json = await httpClient.GetStringAsync(url);
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(json);
            var token = data["data"];
            Token = token.ToString();
            UpFileUrl = $"{Request.Scheme}://{Request.Host.Value}/api/v1/filemanage/upload";
            BaseUrl = $"{Request.Scheme}://{Request.Host.Value}";
            return Page();
        }

        /// <summary>
        /// Handler=Ajax
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetAjaxAsync(int pageIndex = 1, int pageSize = 20)
        {
            var result = await _fileRecordService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Descending, "SysKey");
            var fileRecordDtos = new PagedList<FileRecordDto>(result.Select(z => _fileRecordService.Mapper.Map<FileRecordDto>(z)).ToList(), result.PageIndex, result.PageCount, result.TotalCount);
            return Ok(new { fileRecordDtos.TotalCount, pageIndex, pageSize, list = fileRecordDtos.AsEnumerable() });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostDeleteAsync([FromBody] int[] ids)
        {
            await _fileRecordService.DeleteObjectAsync(d => ids.Contains(d.Id));
            return Ok(ids.Length);
            //return RedirectToPage("./Index");
        }
    }
}
