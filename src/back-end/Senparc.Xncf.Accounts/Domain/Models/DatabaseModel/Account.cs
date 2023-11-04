using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.Accounts.Domain.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.Accounts.Domain.Models
{
    [Serializable]
    [Table("Accounts")]
    public partial class Account : EntityBase<int>
    {
        public Account()
        {
            PointsLogs = new List<PointsLog>();
        }

        public string UserName { get; set; }
        //[MaxLength(100)]
        public string Password { get; set; }
        //[MaxLength(100)]
        public string PasswordSalt { get; set; }
        //[Required,MaxLength(50)]
        public string NickName { get; set; }
        public string RealName { get; set; }
        public string Phone { get; set; }
        public bool? PhoneChecked { get; set; }

        public string Email { get; set; }
        public bool? EmailChecked { get; set; }
        public string PicUrl { get; set; }
        public string HeadImgUrl { get; set; }
        public decimal Package { get; set; }
        public decimal Balance { get; set; }
        public decimal LockMoney { get; set; }
        public byte Sex { get; set; }
        public string QQ { get; set; }
        [MaxLength(30)]
        public string Country { get; set; }
        public string Province { get; set; }
        [MaxLength(30)]
        public string City { get; set; }
        public string District { get; set; }
        public string Address { get; set; }
        public string Note { get; set; }
        public DateTime ThisLoginTime { get; set; }
        public string ThisLoginIp { get; set; }
        public DateTime LastLoginTime { get; set; }
        public string LastLoginIP { get; set; }
        public decimal Points { get; set; }
        public DateTime? LastWeixinSignInTime { get; set; }
        public int WeixinSignTimes { get; set; }
        public string WeixinUnionId { get; set; }
      
        public string WeixinOpenId { get; set; }

        /// <summary>
        /// 是否被锁定（无法登陆） TODO：暂未添加到DTO中
        /// </summary>
        public bool? Locked { get; set; }
        
        public ICollection<PointsLog> PointsLogs { get; set; }
        public ICollection<AccountPayLog> AccountPayLogs { get; set; }
    }
}
