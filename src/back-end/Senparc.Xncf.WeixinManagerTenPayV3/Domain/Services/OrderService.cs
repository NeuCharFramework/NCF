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
    public class OrderService : ServiceBase<Order>
    {
        public OrderService(IRepositoryBase<Order> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

    
    }
}
