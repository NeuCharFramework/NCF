
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Cache;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Cache.CsRedis;
using WorkShop.Xncf.WebApiDemo01.Models.DatabaseModel.VO;
using WorkShop.Xncf.WebApiDemo01.Models.DatabaseModel.Config;
using WorkShop.Xncf.WebApiDemo01.Utils;
using WorkShop.Xncf.WebApiDemo01.Services;
using WorkShop.Xncf.WebApiDemo01.Models.DatabaseModel.Dto;

namespace WorkShop.Xncf.WebApiDemo01.Controllers
{
    /// <summary>
    /// 文件上传接口
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiController]
    [ApiVersion("1")]
    public class ColorController : BaseController
    {
        private readonly ColorService colorService;

        public ColorController(ColorService colorService)
        {
            this.colorService = colorService;
        }

        /// <summary>
        /// 获取当前颜色
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetColorAsync()
        {
            try
            {
                var response = await colorService.ApiGetColorAsync();
                return Success(response);
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        /// <summary>
        /// 设置颜色
        /// </summary>
        /// <param name="type">类型(1-变亮;2-变暗;3-随机;)</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SetColorAsync(int type)
        {
            try
            {
                var response = await colorService.ApiSetColorAsync(type);
                return Success(response);
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }
    }
}
