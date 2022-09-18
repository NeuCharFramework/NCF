using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerTenPayV3.Domain.Models.DatabaseModel
{
    [Table(name: Register.DATABASE_PREFIX + nameof(Order))]
    [Serializable]
    public class Order : EntityBase<int>
    {
        [Required]
        public string BillNo { get; private set; }
        [Required]
        public int ProductId { get; private set; }
        [Required]
        public int UserId { get; private set; }
        [Required]
        public OrderState OrderState { get; private set; }

        public List<Product> Products { get; private set; }

        private Order()
        {

        }

        public Order(string billNo, int productId, int userId, OrderState orderState)
        {
            BillNo = billNo;
            ProductId = productId;
            UserId = userId;
            OrderState = orderState;
        }

        public void ChangeOrderState(OrderState newOrderState)
        {
            OrderState = newOrderState;
        }
    }
}
