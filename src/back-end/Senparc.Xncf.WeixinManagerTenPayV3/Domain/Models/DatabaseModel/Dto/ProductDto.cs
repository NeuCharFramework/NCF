using Senparc.Ncf.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.WeixinManagerTenPayV3.Models.DatabaseModel.Dto
{
    public class ProductDto : DtoBase
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Store { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public bool IsOpen { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string Note { get; set; }

        public ProductDto() { }
    }
}
