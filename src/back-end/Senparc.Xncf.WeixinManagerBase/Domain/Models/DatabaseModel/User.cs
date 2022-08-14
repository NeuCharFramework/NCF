using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.WeixinManagerBase.Domain.Models.DatabaseModel
{
    [Table(Register.DATABASE_PREFIX + nameof(User))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class User : EntityBase<int>
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string MpOpenId { get; set; }
        public string UnionId { get; set; }

        public string NickName { get; set; }

        public int Sex { get; set; }
        /// <summary>
        ///用户的语言，简体中文为zh_CN
        /// </summary>
        public string Language { get; set; }

        public string City { get; set; }

        public string Province { get; set; }

        public string Country { get; set; }
        public string HeadImgUrl { get; set; }

        User()
        {

        }

        public User(string userName, string password, string email, string phone, string mpOpenId, string unionId, string nickName, int sex, string language, string city, string province, string country, string headImgUrl)
        {
            userName ??= GetUserName();

            UserName = userName;
            Password = password;
            Email = email;
            Phone = phone;
            MpOpenId = mpOpenId;
            UnionId = unionId;
            NickName = nickName;
            Sex = sex;
            Language = language;
            City = city;
            Province = province;
            Country = country;
            HeadImgUrl = headImgUrl;
            AddTime = SystemTime.Now.DateTime;
            LastUpdateTime = SystemTime.Now.DateTime;
        }

        /// <summary>
        /// 获取用户名
        /// </summary>
        /// <returns></returns>
        public string GetUserName()
        {
            return Guid.NewGuid().ToString("N");
        }

        public DateTime UpdateLastUpdateTime() {
            var dt = SystemTime.Now.DateTime;
            this.LastUpdateTime = dt;
            return dt;
        }
    }
}
