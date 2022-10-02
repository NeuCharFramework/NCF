using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Weixin.TenPayV3;
using Senparc.Xncf.WeixinManagerTenPayV3.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManagerTenPayV3.Domain.Services;
using Senparc.Xncf.WeixinManagerTenPayV3.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManagerTenPayV3.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerTenPayV3.OHS.Local.AppService
{
    public class ProductAppService : AppServiceBase
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ProductService _productService;

        public ProductAppService(IServiceProvider serviceProvider, ProductService product) : base(serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this._productService = product;
        }

        //[ApiBind]
        [FunctionRender("添加商品", "手动添加商品信息", typeof(Register))]
        public async Task<StringAppResponse> AddAsync(Product_AddRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var productDto = new ProductDto()
                {
                    Name = request.Name,
                    Price = request.Price,
                    Store = request.Store,
                    Description = request.Description,
                    ImageUrl = request.ImageUrl,
                    IsOpen = request.IsOpen.SelectedValues.FirstOrDefault() == "1",
                    OriginalPrice = request.OriginalPrice,
                    Note = request.Note
                };

                var product = new Product(productDto);

                logger.Append("创建新商品：" + product.ToJson());

                await this._productService.SaveObjectAsync(product);

                var productStored = await this._productService.GetProductAsync(product.Id);

                logger.Append("商品保存完毕：" + productStored.ToJson(true));

                return productStored.ToJson(true);
            });
        }

    }
}
