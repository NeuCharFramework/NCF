using Senparc.Ncf.Repository;
using Senparc.Weixin.MP.AdvancedAPIs.User;
using Senparc.Xncf.WeixinManagerBase.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManagerBase.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerMP.Domain.Services
{
    public class MpUserService : UserService
    {
        private readonly string _appId;

        public MpUserService(IRepositoryBase<User> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            _appId = Senparc.Weixin.Config.SenparcWeixinSetting.MpSetting.WeixinAppId;
        }

        /// <summary>
        /// 同步微信公众号用户
        /// </summary>
        /// <returns></returns>
        public async Task<int> SyncMpUser()
        {
            var changedCount = 0;

            //获取用户信息列表
            var userOpenIdResult = await Senparc.Weixin.MP.AdvancedAPIs.UserApi.GetAsync(_appId, null);

            ////方案已（select ... =）：
            //foreach (var openId in userOpenIdResult.data.openid)
            //{
            //    var user = await base.GetObjectAsync(z => z.MpOpenId == openId, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);
            //}

            //方案二（select ... in）：
            var userListTask = base.GetFullListAsync(z => userOpenIdResult.data.openid.Contains(z.MpOpenId));

            //获取用户详细信息的查询条件
            var userInfoQueryList = userOpenIdResult.data.openid.Select(z => new BatchGetUserInfoData()
            {
                LangEnum = Weixin.Language.zh_CN,
                lang = Weixin.Language.zh_CN.ToString(),
                openid = z
            }).ToList();

            //获取微信远程用户详细信息
            var userInfoTask = Senparc.Weixin.MP.AdvancedAPIs.UserApi.BatchGetUserInfoAsync(_appId, userInfoQueryList);

            Task.WaitAll(userListTask, userInfoTask);

            var userInfoResult = userInfoTask.Result.user_info_list;

            List<User> changedUserList = new List<User>();
            //遍历已存在的用户
            foreach (var user in userListTask.Result)
            {
                //用户的对比
                var userInfo = userInfoResult.FirstOrDefault(z => z.openid == user.MpOpenId);

                if (userInfo == null)
                {
                    //用户已经被删除（删除记录或标注）

                }

                var changed = user.UpdateUser(userInfo.nickname, userInfo.headimgurl);

                if (changed)
                {
                    changedUserList.Add(user);
                }
            }

            if (changedUserList.Count > 0)
            {
                //TODO try
                await base.SaveObjectListAsync(changedUserList);
                changedCount += changedUserList.Count;
            }

            //遍历不存在的用户（新增）
            var newUserInfo = userInfoResult
                    .Where(z =>
                        !userListTask.Result.Exists(u => u.MpOpenId == z.openid))
                    .ToList();
            foreach (var userInfo in newUserInfo)
            {
                await base.CreateUserAsync(null, "", mpOpenId: userInfo.openid, unionId: userInfo.unionid);
                changedCount++;
            }

            //遍历取消关注的用户（删除/标记）

            return changedCount;
        }
    }
}
