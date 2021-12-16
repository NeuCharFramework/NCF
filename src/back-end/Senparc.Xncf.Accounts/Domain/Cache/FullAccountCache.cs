using Senparc.Ncf.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Senparc.Xncf.Accounts.Domain.Cache
{
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Senparc.CO2NET;
    using Senparc.CO2NET.Extensions;
    using Senparc.Xncf.Accounts.Domain.Cache;
    using Senparc.Ncf.Core.DI;
    using Senparc.Ncf.Core.Enums;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Senparc.Ncf.Core.Models;
    using Senparc.Ncf.Core.Cache;

    [AutoDIType(DILifecycleType.Scoped)]
    public class FullAccountCache : BaseStringDictionaryCache<FullAccount, Account> //, IFullAccountCache
    {
        /// <summary>
        /// UserId和UserName的映射关系
        /// </summary>
        public static ConcurrentDictionary<int, string> UserIdNameRelationshop = new ConcurrentDictionary<int, string>();

        public const string CACHE_KEY = "FullAccountCache";
        private const int timeout = 1440;

        private INcfDbData _dataContext => base._db as INcfDbData;

        #region 同步方法

        /// <summary>
        /// 根据判断条件获取User
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        private Account GetAccount(Expression<Func<Account, bool>> where)
        {
            var account = (_dataContext.BaseDataContext as SenparcEntitiesBase).Set<Account>()
                .FirstOrDefault(where);
            return account;
        }

        public FullAccountCache(INcfDbData db)
            : base(CACHE_KEY, db, timeout)
        {
        }

        public override FullAccount Update()
        {
            return null;
        }

        public override FullAccount InsertObjectToCache(string key)
        {
            var account = this.GetAccount(z => z.UserName.Equals(key, StringComparison.OrdinalIgnoreCase));
            var fullUser = this.InsertObjectToCache(key, account);
            return fullUser;
        }

        public override FullAccount GetObject(string key)
        {
            return base.GetObject(key);
        }

        public override FullAccount InsertObjectToCache(string key, Account obj)
        {
            var fullUser = base.InsertObjectToCache(key, obj);
            if (fullUser == null)
            {
                return null;
            }
            UserIdNameRelationshop[fullUser.Id] = fullUser.UserName;//TODO:需要使用分布式缓存

            this.UpdateToCache(key, fullUser); //需要更新对象到Redis
            return fullUser;
        }

        public void ForceLogout(string userName)
        {
            if (userName.IsNullOrEmpty())
            {
                return;
            }
            var fullUser = GetObject(userName);
            if (fullUser == null)
            {
                return;
            }
            fullUser.ForceLogout = true;
            fullUser.LastActiveTime = DateTime.MinValue;
        }

        public string GetUserName(int accountId)
        {
            var userName = UserIdNameRelationshop.ContainsKey(accountId)
                ? UserIdNameRelationshop[accountId]
                : null;
            return userName;
        }
        public FullAccount GetFullAccount(int accountId)
        {
            var userName = GetUserName(accountId);
            if (userName == null)
            {
                //未命中，查找数据库
                var account = this.GetAccount(z => z.Id == accountId);
                if (account == null)
                {
                    return null;
                }
                var fullUser = this.InsertObjectToCache(account.UserName, account);
                return fullUser;
            }
            return GetObject(userName);
        }

        public override void RemoveCache()
        {
            throw new Exception("不允许调用此方法！");
        }


        #endregion


        #region 异步方法


        /// <summary>
        /// 根据判断条件获取User
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        private async Task<Account> GetAccountAsync(Expression<Func<Account, bool>> where)
        {
            var account = await (_dataContext.BaseDataContext as SenparcEntitiesBase).Set<Account>()
                            .FirstOrDefaultAsync(where).ConfigureAwait(false);
            return account;
        }


        public override async Task<FullAccount> InsertObjectToCacheAsync(string key, Account obj)
        {
            var fullUser = await base.InsertObjectToCacheAsync(key, obj);
            if (fullUser == null)
            {
                return null;
            }
            UserIdNameRelationshop[fullUser.Id] = fullUser.UserName;//TODO:需要使用分布式缓存

            await this.UpdateToCacheAsync(key, fullUser).ConfigureAwait(false); //需要更新对象到Redis
            return fullUser;
        }

        public override async Task<FullAccount> InsertObjectToCacheAsync(string key)
        {
            var account = await this.GetAccountAsync(z => z.UserName.Equals(key, StringComparison.OrdinalIgnoreCase));
            var fullUser = await this.InsertObjectToCacheAsync(key, account);
            return fullUser;
        }
        #endregion
    }
}