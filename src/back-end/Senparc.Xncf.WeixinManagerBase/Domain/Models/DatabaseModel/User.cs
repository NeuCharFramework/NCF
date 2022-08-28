using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;
using System.Linq.Expressions;
using Senparc.CO2NET.Extensions;

namespace Senparc.Xncf.WeixinManagerBase.Domain.Models.DatabaseModel
{
    [Table(Register.DATABASE_PREFIX + nameof(User))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class User : EntityBase<int>
    {
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public string Email { get; private set; }
        public string Phone { get; private set; }
        public string MpOpenId { get; private set; }
        public string UnionId { get; private set; }

        public string NickName { get; private set; }

        public int Sex { get; private set; }
        /// <summary>
        ///用户的语言，简体中文为zh_CN
        /// </summary>
        public string Language { get; private set; }

        public string City { get; private set; }

        public string Province { get; private set; }

        public string Country { get; private set; }
        public string HeadImgUrl { get; private set; }

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

        public DateTime UpdateLastUpdateTime()
        {
            var dt = SystemTime.Now.DateTime;
            this.LastUpdateTime = dt;
            return dt;
        }

        public bool UpdateUser(string nickName, string headImg)
        {
            bool changed = false;

            if (!nickName.IsNullOrEmpty() && NickName != nickName)
            {
                changed = changed || true;
                NickName = nickName;
            }

            if (!headImg.IsNullOrEmpty() && HeadImgUrl != headImg)
            {
                changed = changed || true;
                HeadImgUrl = headImg;
            }

            //UpdateElement(z => z.HeadImgUrl, headImg);

            return changed;
        }


        //private bool UpdateElement(Func<Expression<User>,object> )
    }
}
