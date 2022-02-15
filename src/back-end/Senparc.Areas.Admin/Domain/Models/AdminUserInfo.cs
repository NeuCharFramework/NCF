using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models
{
    [Table(Register.DATABASE_PREFIX + nameof(AdminUserInfo) + "s")]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public partial class AdminUserInfo : EntityBase<int>
    {
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public string PasswordSalt { get; private set; }
        public string RealName { get; private set; }
        public string Phone { get; private set; }
        public string Note { get; private set; }
        public DateTime ThisLoginTime { get; private set; }
        public string ThisLoginIp { get; private set; }
        public DateTime LastLoginTime { get; private set; }
        public string LastLoginIp { get; private set; }

        private AdminUserInfo() { }

        public AdminUserInfo(ref string userName, ref string password, string realName, string phone, string note)
        {
            userName ??= GenerateUserName();//记录用户名
            password ??= GeneratePassword();//记录明文密码

            UserName = userName;
            PasswordSalt = GeneratePasswordSalt();//生成密码盐
            Password = GetMD5Password(password, PasswordSalt, false);//生成密码
            RealName = realName;
            Phone = phone;
            Note = note;

            var now = SystemTime.Now.LocalDateTime;
            AddTime = now;
            ThisLoginTime = now;
            LastLoginTime = now;

            //TODO：用户名及密码合规性验证
        }

        /// <summary>
        /// 生成用户名
        /// </summary>
        /// <returns></returns>
        public string GenerateUserName()
        {
            return $"SenparcCoreAdmin{new Random().Next(100).ToString("00")}";
        }

        /// <summary>
        /// 生成随机密码
        /// </summary>
        /// <returns></returns>
        public string GeneratePassword()
        {
            return Guid.NewGuid().ToString("n").Substring(0, 8);//TODO:更强的代码策略，或自定义的代码策略
        }

        /// <summary>
        /// 生成密码盐
        /// </summary>
        /// <returns></returns>
        public string GeneratePasswordSalt()
        {
            return DateTime.Now.Ticks.ToString();
        }

        /// <summary>
        /// 获取加盐后的 MD5 密码
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <param name="isMD5Password"></param>
        /// <returns></returns>
        public string GetMD5Password(string password, string salt, bool isMD5Password)
        {
            string md5 = password.ToUpper().Replace("-", "");
            if (!isMD5Password)
            {
                md5 = MD5.GetMD5Code(password, "").Replace("-", ""); //原始MD5
            }
            return MD5.GetMD5Code(md5, salt).Replace("-", ""); //再加密
        }

        public void UpdateObject(CreateOrUpdate_AdminUserInfoDto objDto)
        {
            UserName = objDto.UserName;
            if (!objDto.Password.IsNullOrEmpty())
            {
                Password = GetMD5Password(objDto.Password, this.PasswordSalt, false);
            }

            RealName = objDto.RealName;
            Phone = objDto.Phone;
            Note = objDto.Note;

            base.SetUpdateTime();
        }
    }
}
