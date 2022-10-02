using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Ncf.XncfBase.Functions;

namespace Senparc.Xncf.WeixinManagerTenPayV3.OHS.Local.PL
{
    public class Product_AddRequest : FunctionAppRequestBase
    {
        [Required]
        [Description("产品名称||100字以内")]
        public string Name { get; set; }
        [Required]
        [Description("产品价格||必须大于0")]
        public decimal Price { get; set; }
        [Description("原价||必须大于0")]
        public decimal? OriginalPrice { get; set; }
        [Required]
        [Description("产品库存||产品库存")]
        public int Store { get; set; }
        [Required]
        [Description("产品介绍||产品介绍，最多5000字")]
        public string Description { get; set; }
        [Required]
        [Description("产品图片||请输入URL")]
        public string ImageUrl { get; set; }
        [Required]
        [Description("是否开放")]
        public SelectionList IsOpen { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("0","否","商品不上架，不可交易",false),
                 new SelectionItem("1","是","商品上架，可交易",true)
            });

        [Description("备注||商品备注")]
        public string Note { get; set; }
    }
}
