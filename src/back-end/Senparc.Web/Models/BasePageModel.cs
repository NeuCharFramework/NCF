using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Web.Models.VD
{
    public interface IBasePageModel : IPageModelBase
    { }

    /// <summary>
    /// 当前项目供前端（非Areas）使用的PageModel全局基类
    /// </summary>
    public class BasePageModel : PageModelBase, IBasePageModel
    {
    }
}
