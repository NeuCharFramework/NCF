using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManagerTenPayV3.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManagerTenPayV3.Domain.Services;
using Senparc.Xncf.WeixinManagerTenPayV3.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerTenPayV3.Domain.Services
{
    public class ProductService : ServiceBase<Product>
    {
        public ProductService(IRepositoryBase<Product> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        public async Task<ProductDto> GetProductAsync(int id)
        {
            var product = await base.GetObjectAsync(z => z.Id == id);
            if (product == null)
            {
                return null;
            }

            return base.Mapper.Map<ProductDto>(product);
        }
    }
}
