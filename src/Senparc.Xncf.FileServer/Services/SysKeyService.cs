using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.FileServer.Models.DatabaseModel;
using Senparc.Xncf.FileServer.Models.DatabaseModel.Dto;
using Senparc.Xncf.FileServer.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileServer.Services
{
    /// <summary>
    /// 使用这种服务类型扩展时需要使用模块上下文
    /// </summary>
    public class SysKeyService : ServiceBase<SysKey>
    {
        private readonly IOptionsMonitor<SafeSetting> safeSetting;
        private FileServerSenparcEntities _fileServerSenparcEntities;
        private IMemoryCache _cache;
        public SysKeyService(IRepositoryBase<SysKey> repo, IServiceProvider serviceProvider, FileServerSenparcEntities fileServerSenparcEntities, IOptionsMonitor<SafeSetting> safeSetting, IMemoryCache cache)
            : base(repo, serviceProvider)
        {
            this.safeSetting = safeSetting;
            this._fileServerSenparcEntities = fileServerSenparcEntities;
            _cache = cache;
        }

        /// <summary>
        /// 创建App
        /// </summary>
        /// <param name="Password"></param>
        /// <param name="AppId"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        public async Task<SysKeyDto> CreateApp(string Password, string AppId, string Name)
        {
            if (!safeSetting.CurrentValue.IsDev)
            {
                throw new Exception("只有开发环境下才可使用此接口！");
            }
            if (string.IsNullOrWhiteSpace(Password) || Password != safeSetting.CurrentValue.CreateAppPassword)
            {
                throw new Exception("密码错误！");
            }
            var exist = base.GetCount(i => i.Name == Name) > 0;
            if (exist)
            {
                throw new Exception("提供APP名称已存在！");
            }
            var exist2 = base.GetCount(i => i.AppId == AppId) > 0;
            if (exist2)
            {
                throw new Exception("AppId已存在！");
            }
            var syskeyModel = new SysKey()
            {
                AppId = AppId,
                AppKey = UniqueHelper.LongId().ToString() + UniqueHelper.LongId().ToString(),
                Name = Name
            };
            await base.SaveObjectAsync(syskeyModel);
            return base.Mapper.Map<SysKeyDto>(syskeyModel);
        }

        /// <summary>
        /// 获取App列表
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<IEnumerable<SysKeyDto>> GetAppList(int pageNumber = 1, int pageSize = 20)
        {
            var list = await base.GetObjectListAsync(pageNumber, pageSize, i => true, "CreateTime desc");
            return base.Mapper.Map<IEnumerable<SysKeyDto>>(list);
        }

        public async Task<string> GetToken(string AppId, string AppKey)
        {
            var sysKeyModel = await base.GetObjectAsync(i => i.AppId == AppId && i.AppKey == AppKey);
            if (sysKeyModel == null)
            {
                throw new Exception("无效的AppId、AppKey！");
            }
            //增加缓存
            var cacheEntry = _cache.GetOrCreate<string>(AppId, cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(safeSetting.CurrentValue.TokenCache * 60);//单位分
                var token = new JwtBuilder()
                     .WithAlgorithm(new HMACSHA256Algorithm())
                     .WithSecret(safeSetting.CurrentValue.Secret)
                     .AddClaim("exp", DateTimeOffset.UtcNow.AddMinutes(safeSetting.CurrentValue.TokenExpired).ToUnixTimeSeconds())
                     .AddClaim("id", sysKeyModel.Id)
                     //.AddClaim("appkey", AppKey)
                     .Encode();
                return token;
            });
            return cacheEntry;
        }

        public async Task<SysKeyDto> ValidToken(string token)
        {
            try
            {
                var payload = new JwtBuilder()
                    .WithAlgorithm(new HMACSHA256Algorithm())
                    .WithSecret(safeSetting.CurrentValue.Secret)
                    .MustVerifySignature()
                    .Decode<IDictionary<string, int>>(token);
                var id = payload["id"];
                var syskeyModel = await base.GetObjectAsync(i => i.Id == id);
                return base.Mapper.Map<SysKeyDto>(syskeyModel);
            }
            catch (TokenExpiredException)
            {
                throw new Exception("令牌过期！");
            }
            catch (SignatureVerificationException)
            {
                throw new Exception("签名校验异常！");
            }
        }
        //TODO: 更多业务方法可以写到这里
    }
}
