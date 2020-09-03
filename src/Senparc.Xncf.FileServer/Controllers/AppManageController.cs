using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Senparc.Xncf.FileServer.Models.DatabaseModel.Dto;
using Senparc.Xncf.FileServer.Models.VD;
using Senparc.Xncf.FileServer.Services;
using Senparc.Xncf.FileServer.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Senparc.Xncf.FileServer.Controllers
{
    /// <summary>
    /// App相关
    /// </summary>
    //[Route("api/v{version:apiVersion}/[controller]/[action]")]
    //[Route("AppManage")]
    [Route("v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    public class AppManageController : Controller
    {
        private readonly SysKeyService _sysKeyService;
        public AppManageController(SysKeyService sysKeyService)
        {
            _sysKeyService = sysKeyService;
        }

        /// <summary>
        /// 创建APP
        /// </summary>
        /// <param name="password">创建密码</param>
        /// <param name="appname">创建APP名称</param>
        /// <returns></returns>
        [HttpGet]
        [Route("CreateApp")]
        public async Task<ActionResultVD<SysKeyDto>> CreateApp([BindRequired] string password, [BindRequired] string appname)
        {
            var vd = new ActionResultVD<SysKeyDto>();
            try
            {
                vd.Data = await _sysKeyService.CreateApp(password, UniqueHelper.LongId().ToString(), appname);
                vd.Set(ARTag.success);
            }
            catch (Exception ex)
            {
                vd.Set(ex);
            }
            return vd;
        }

        /// <summary>
        /// 获取AppId的注册token
        /// </summary>
        /// <param name="AppId">注册的AppId</param>
        /// <param name="AppKey">注册的AppKey</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetToken")]
        public async Task<ActionResultVD<string>> GetToken([BindRequired] string AppId, [BindRequired] string AppKey)
        {
            var vd = new ActionResultVD<string>();
            try
            {
                vd.Data = await _sysKeyService.GetToken(AppId, AppKey);
                vd.Set(ARTag.success);
            }
            catch (Exception ex)
            {
                vd.Set(ex);
            }
            return vd;
        }

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        /// <param name="token">需要验证的token</param>
        /// <returns></returns>
        [HttpGet]
        [Route("ValidToken")]
        public async Task<ActionResultVD<SysKeyDto>> ValidToken([BindRequired] string token)
        {
            var vd = new ActionResultVD<SysKeyDto>();
            try
            {
                vd.Data = await _sysKeyService.ValidToken(token);
                vd.Set(ARTag.success);
            }
            catch (Exception ex)
            {
                vd.Set(ex);
            }
            return vd;
        }

        /// <summary>
        /// 获取App列表
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageCount"></param>
        /// <returns></returns>
        public async Task<ActionResultVD<IEnumerable<SysKeyDto>>> GetList(int pageIndex = 0, int pageCount = 20)
        {
            var vd = new ActionResultVD<IEnumerable<SysKeyDto>>();
            try
            {
                vd.Data = await _sysKeyService.GetAppList(pageIndex, pageCount);
                vd.Set(ARTag.success);
            }
            catch (Exception ex)
            {
                vd.Set(ex);
            }
            return vd;
        }
    }
}
