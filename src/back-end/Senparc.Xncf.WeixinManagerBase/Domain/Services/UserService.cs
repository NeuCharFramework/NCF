using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManagerBase.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManagerBase.Domain.Models.DatabaseModel.Dto;
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


        public async Task<User_CreateOrUpdateDto> CreateOrUpdateFromOAuthAsync(string openid, string nickname, string headimgurl, string unionid)
        {
            var user = base.GetObject(z => z.MpOpenId == openid);
            if (user == null)
            {
                user = await this.CreateUserAsync(null,"",mpOpenId:openid, nickName: nickname, headImgUrl: headimgurl, unionId: unionid).ConfigureAwait(false);
            }
            else
            {
                //TODO:进行更新
                user.UpdateLastUpdateTime();
            }

            await base.SaveObjectAsync(user);
            return base.Mapper.Map<User_CreateOrUpdateDto>(user);
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
