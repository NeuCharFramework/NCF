using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerTenPayV3.Domain.Models
{
    /// <summary>
    /// 订单状态
    /// </summary>
    public enum OrderState
    {
        待支付 = 0,
        已取消 = 1,
        已支付 = 2,
        已发货 = 3,
        已完成 = 4,
        已退款 = 5,
        已关闭 = 6,
    }
}
