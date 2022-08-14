using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManagerBase.Domain.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerBase.Domain.Services
{
    public class UserService : ServiceBase<User>
    {
        public UserService(IRepositoryBase<User> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        /// <returns></returns>
        public async Task<User> CreateUserAsync(string userName, string password, string email = null, string phone = null, string mpOpenId = null, string unionId = null, string nickName = null, int sex = 0, string language = null, string city = null, string province = null, string country = null, string headImgUrl = null)
        {
            var user = new User(userName, password, email, phone, mpOpenId, unionId, nickName, sex, language, city, province, country, headImgUrl);

            await base.SaveObjectAsync(user);
            return user;
        }

        public async Task<DateTime> UpdateLastUpdateTime(User user)
        {
            return user.UpdateLastUpdateTime();

        }
    }
}
