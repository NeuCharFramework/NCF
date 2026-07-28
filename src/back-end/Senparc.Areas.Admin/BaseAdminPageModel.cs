/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BaseAdminPageModel.cs
    文件功能描述：BaseAdminPageModel.cs 相关实现
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.Areas.Admin.Domain.Models.VD;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Models.VD;
using Senparc.Ncf.Core.WorkContext;
using Senparc.Ncf.XncfBase;
using Senparc.Weixin.WxOpen.AdvancedAPIs.DataCube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin
{
    public interface IBaseAdminPageModel : IBasePageModel
    {

    }

    //暂时取消权限验证
    [ServiceFilter(typeof(AuthenticationResultFilterAttribute))]
    [AdminAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class BaseAdminPageModel : AdminPageModelBase, IBaseAdminPageModel
    {
        public Senparc.Areas.Admin.Register _xncfRegister;
        protected readonly IServiceProvider _serviceProvider;

        public Senparc.Areas.Admin.Register XncfRegister
        {
            get
            {
                _xncfRegister = _xncfRegister ?? new Register();
                return _xncfRegister;
            }
        }


        public BaseAdminPageModel(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            //context
            if (!context.ModelState.IsValid)
            {
                //全局模型验证
                var state = context.ModelState
                    .Where(_ => _.Value.ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
                    .Select(_ => new { _.Key, Errors = _.Value.Errors.Select(__ => __.ErrorMessage) });
                context.Result = BadRequest(new AjaxReturnModel<object>(state));
            }
            base.OnPageHandlerExecuting(context);
        }

        public override IActionResult RenderError(string message)
        {
            return base.RenderError(message);
        }
    }
}
