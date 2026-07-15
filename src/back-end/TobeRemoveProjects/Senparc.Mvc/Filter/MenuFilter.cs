using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.Ncf.Core.Models.VD;

namespace Senparc.Mvc.Filter
{
    public class MenuFilterAttribute : ActionFilterAttribute
    {
        public string CurrentMenu { get; set; }
        public MenuFilterAttribute(string currentMenu)
        {
            CurrentMenu = currentMenu;
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if ((filterContext.Controller as Controller).ViewData.Model is IBaseUiVD model)
            {
                //model.CurrentMenu = model.CurrentMenu ?? CurrentMenu;
                model.CurrentMenu = CurrentMenu;
            }
            base.OnResultExecuting(filterContext);
        }
    }
}
