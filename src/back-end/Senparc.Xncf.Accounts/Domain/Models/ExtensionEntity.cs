using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.Accounts.Domain.Models
{

    /// <summary>
    /// 用户信息
    /// </summary>
    [Serializable]
    public partial class FullAccountBase : BaseFullEntity<Account>
    {
        public override string Key => UserName;

        [AutoSetCache]
        public int Id { get; set; }

        [AutoSetCache]
        public string UserName { get; set; }
        public DateTime LastActiveTime { get; set; }
        public DateTime LastOpenPageTime { get; set; }
        public string LastOpenPageUrl { get; set; }
        public string LastActiveUserAgent { get; set; }
        ///// <summary>
        ///// 最近一次活动的客户端设备信息
        ///// </summary>
        //public IDevice LastActiveDevice
        //{
        //    get
        //    {
        //        if (!LastActiveUserAgent.IsNullOrEmpty())
        //        {
        //            var wurflManager = WurflUtility.WurflManager;
        //            var device = wurflManager.GetDeviceForRequest(LastActiveUserAgent);
        //            return device;
        //        }
        //        return null;
        //    }
        //}

        public bool IsLogined => Id > 0 && !UserName.IsNullOrEmpty() && (DateTime.Now - LastActiveTime).TotalMinutes < 2;

        /// <summary>
        /// 强制退出登录
        /// </summary>
        public bool ForceLogout { get; set; }

        /// <summary>
        /// 未读消息数量
        /// </summary>
        public int UnReadMessageCount { get; set; }

        /// <summary>
        /// 已经输入过验证码（如果需要加强验证，可以加上次输入验证码的时间以及token）
        /// </summary>
        public bool CheckCodePassed { get; set; }

        /// <summary>
        /// 在线图标
        /// </summary>
        public string OnlineImg => $"/Content/Images/{(IsLogined ? "online" : "offline")}.png";

        public FullAccountBase()
        {
            LastActiveTime = DateTime.MinValue;
            LastOpenPageTime = DateTime.MinValue;
        }
    }

    [Serializable]
    public class FullAccount : FullAccountBase
    {
        [AutoSetCache]
        public string Password { get; set; }

        [AutoSetCache]
        public string PasswordSalt { get; set; }

        [AutoSetCache]
        public string NickName { get; set; }

        [AutoSetCache]
        public string RealName { get; set; }

        [AutoSetCache]
        public string Email { get; set; }

        [AutoSetCache]
        public bool? EmailChecked { get; set; }

        [AutoSetCache]
        public string Phone { get; set; }

        [AutoSetCache]
        public bool? PhoneChecked { get; set; }

        [AutoSetCache]
        public string WeixinOpenId { get; set; }

        [AutoSetCache]
        public string PicUrl { get; set; }

        [AutoSetCache]
        public string HeadImgUrl { get; set; }

        [AutoSetCache]
        public decimal Package { get; set; }

        [AutoSetCache]
        public decimal Balance { get; set; }

        [AutoSetCache]
        public decimal LockMoney { get; set; }

        [AutoSetCache]
        public byte Sex { get; set; }

        [AutoSetCache]
        public string QQ { get; set; }

        [AutoSetCache]
        public string Country { get; set; }

        [AutoSetCache]
        public string Province { get; set; }

        [AutoSetCache]
        public string City { get; set; }

        [AutoSetCache]
        public string District { get; set; }

        [AutoSetCache]
        public string Address { get; set; }

        [AutoSetCache]
        public string Note { get; set; }

        [AutoSetCache]
        public System.DateTime ThisLoginTime { get; set; }

        [AutoSetCache]
        public string ThisLoginIp { get; set; }

        [AutoSetCache]
        public System.DateTime LastLoginTime { get; set; }

        [AutoSetCache]
        public string LastLoginIP { get; set; }

        [AutoSetCache]
        public System.DateTime AddTime { get; set; }

        [AutoSetCache]
        public decimal Points { get; set; }

        [AutoSetCache]
        public DateTime? LastWeixinSignInTime { get; set; }

        [AutoSetCache]
        public int WeixinSignTimes { get; set; }

        [AutoSetCache]
        public string WeixinUnionId { get; set; }

        /// <summary>
        /// 账户显示名称
        /// </summary>
        public string DisplayName => NickName ?? UserName;

        public override void CreateEntity(Account entity)
        {
            base.CreateEntity(entity);
        }

    }

}
