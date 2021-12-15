using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.Areas.Admin.Domain.Models.VD;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Models.VD;
using Senparc.Ncf.Core.WorkContext;
using Senparc.Ncf.XncfBase;
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
    [AdminAuthorize("AdminOnly")]
    public class BaseAdminPageModel : AdminPageModelBase, IBaseAdminPageModel
    {
        public Senparc.Areas.Admin.Register _xncfRegister;
        public Senparc.Areas.Admin.Register XncfRegister
        {
            get
            {
                _xncfRegister = _xncfRegister ?? new Register();
                return _xncfRegister;
            }
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
