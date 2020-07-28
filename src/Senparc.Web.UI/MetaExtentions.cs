using Senparc.Ncf.Core.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Html;
using Senparc.Core.Models.VD;

namespace System.Web.Mvc
{
    public static class MetaExtentions
    {
        public static HtmlString RenderMeta(this HtmlHelper helper)
        {
            if (!(helper.ViewData.Model is IBaseVD))
            {
                return new HtmlString("");
            }

            IBaseVD model = helper.ViewData.Model as IBaseVD;
            MetaCollection metaCollection = model.MetaCollection as MetaCollection;
            string result = null;
            foreach (var item in metaCollection)
            {
                if (!string.IsNullOrEmpty(item.Value))
                {
                    result += string.Format("<meta name=\"{0}\" content=\"{1}\" />\r\n", item.Key.ToString(), 
                        //helper.AttributeEncode(item.Value) //COCONET 此方法已失效
                        item.Value
                        );
                }
            }
            return new HtmlString(result);
        }
    }
}
