using Senparc.Areas.Admin.Domain.Dto;
using Senparc.CO2NET;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Cache.Extensions;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;
using Senparc.Xncf.SystemManager.Domain.Service;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    [BackendJwtAuthorize]
    public class SystemInfoAppService : LocalAppServiceBase
    {
        private readonly SystemConfigService _systemConfigService;
        private readonly FullSystemConfigCache _fullSystemConfigCache;
        private readonly IBaseObjectCacheStrategy _cacheStrategy;

        public SystemInfoAppService(IServiceProvider serviceProvider, SystemConfigService systemConfigService, FullSystemConfigCache fullSystemConfigCache, IBaseObjectCacheStrategy cacheStrategy) : base(serviceProvider)
        {
            _systemConfigService = systemConfigService;
            _fullSystemConfigCache = fullSystemConfigCache;
            this._cacheStrategy = cacheStrategy;
        }


        /// <summary>
        /// 获取全部系统信息
        /// </summary>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        public async Task<AppResponseBase<List<SystemConfigDto>>> GetListAsync()
        {
            var response = await this.GetResponseAsync<AppResponseBase<List<SystemConfigDto>>, List<SystemConfigDto>>(async (response, logger) =>
            {
                var list = await _systemConfigService.GetFullListAsync(z => true);
                var dtoList = list.Select(z => _systemConfigService.Mapper.Map<SystemConfigDto>(z)).ToList();
                return dtoList;
            });
            return response;
        }


        /// <summary>
        /// 创建or修改系统信息
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="Ncf.Core.Exceptions.NcfExceptionBase"></exception>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        //[Permission(Codes = new string[] { "code" })]
        //[Ncf.Core.Authorization.Permission("role.add,role.update")]
        public async Task<AppResponseBase<SystemConfigDto>> CreateOrUpdateAsync(SystemConfig_CreateOrUpdateDto request)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SystemConfigDto>, SystemConfigDto>(async (response, logger) =>
            {
                var systemConfig = await _systemConfigService.GetObjectAsync(z => z.Id == request.Id);

                if (systemConfig == null)
                {
                    throw new NcfExceptionBase("系统配置信息不存在");
                }

                systemConfig.Update(request.SystemName, request.MchId, request.MchKey, request.TenPayAppId, systemConfig.HideModuleManager);

                await _systemConfigService.SaveObjectAsync(systemConfig);

                var systemConfigDto = _systemConfigService.Mapper.Map<SystemConfigDto>(systemConfig);

                return systemConfigDto;
            });
            return response;
        }

        /// <summary>
        /// 打开或关闭 模块管理
        /// </summary>
        /// <param name="id"></param>
        /// <param name="hide">是否隐藏管理模块</param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        //[Permission(Codes = new string[] { "code" })]
        //[Ncf.Core.Authorization.Permission("role.add,role.update")]
        public async Task<AppResponseBase<SystemConfigDto>> HideManagerAsync(int id, bool hide)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SystemConfigDto>, SystemConfigDto>(async (response, logger) =>
            {
                var seh = new SenparcExpressionHelper<SystemConfig>();
                seh.ValueCompare.AndAlso(id > 0, z => z.Id == id);
                var sehWhere = seh.BuildWhereExpression();

                var systemConfig = await _systemConfigService.GetObjectAsync(sehWhere);

                if (systemConfig == null)
                {
                    throw new NcfExceptionBase("系统配置信息不存在");
                }

                systemConfig.Update(systemConfig.SystemName, systemConfig.MchId, systemConfig.MchKey, systemConfig.TenPayAppId, hide);

                await _systemConfigService.SaveObjectAsync(systemConfig);

                var systemConfigDto = _systemConfigService.Mapper.Map<SystemConfigDto>(systemConfig);

                return systemConfigDto;
            });
            return response;
        }

        [FunctionRender("缓存测试", "测试当前缓存类型及分布式锁", typeof(Register))]
        public async Task<StringAppResponse> CacheTest()
        {
            var response = await this.GetStringResponseAsync(async (response, logger) =>
            {
                logger.Append("当前缓存：" + _cacheStrategy.GetType().Name);
                var key = "NCF.CacheTest";
                var dt1 = SystemTime.Now;


                await _cacheStrategy.SetAsync(key, Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting, TimeSpan.FromSeconds(1));
                var dt2 = SystemTime.Now;
                logger.Append($"写入耗时：{(dt2 - dt1).TotalMilliseconds}ms");

                var settingFromCache = await _cacheStrategy.GetAsync<SenparcCoreSetting>(key);
                var dt3 = SystemTime.Now;
                logger.Append($"写入耗时：{(dt3 - dt2).TotalMilliseconds}ms");

                Thread.Sleep(2000);
                var settingFromCache2 = await _cacheStrategy.GetAsync<SenparcCoreSetting>(key);
                logger.Append($"缓存自动过期：{settingFromCache2 == null}");
                var index = 0;
                logger.Append($"缓存锁测试开始");

                //清理缓存
                await _cacheStrategy.RemoveFromCacheAsync("NcfLockTest");

                Parallel.For(0, 10, async i =>
                    {

                        using (var cacheLock = await _cacheStrategy.BeginCacheLockAsync("SystemInfoAppService", "CacheTest", 100, TimeSpan.FromMilliseconds(100)))
                        {

                            var cacheObjects = await _cacheStrategy.GetAsync<List<LockCacheTest>>("NcfLockTest");
                            if (cacheObjects == null)
                            {
                                cacheObjects = new List<LockCacheTest>();
                            }

                            cacheObjects.Add(new LockCacheTest(index, SystemTime.Now.DateTime, i));

                            await _cacheStrategy.SetAsync("NcfLockTest", cacheObjects, TimeSpan.FromMinutes(10));
                            logger.Append($"缓存已写入，i:{i}, index:{index}");
                            //Thread.Sleep(1500);
                            index++;
                        }
                    });
                var cacheObjects = await _cacheStrategy.GetAsync<List<LockCacheTest>>("NcfLockTest");
                logger.Append($"缓存锁测试结束，lastLockCache:{cacheObjects.ToJson(true)}");

                //分级缓存测试
                for (int i = 0; i < 10; i++)
                {
                    var cacheKey = $"NcfLockTestLevels:{i}";
                    await _cacheStrategy.RemoveFromCacheAsync(cacheKey);
                    await _cacheStrategy.SetAsync(cacheKey, i, TimeSpan.FromMinutes(10));
                }
                var cacheObjectLevels = await _cacheStrategy.GetAllByPrefixAsync<dynamic>("NcfLockTestLevels");
                logger.Append($"分层索引，NcfLockTestLevels:{cacheObjectLevels.ToJson(true)}");

                logger.SaveLogs("缓存测试");
                return logger.GetLogs();
            });
            return response;
        }

        class LockCacheTest
        {
            public LockCacheTest(int index, DateTime dateTime, int i)
            {
                Index = index;
                DateTime = dateTime;
                I = i;
            }

            public int Index { get; set; }
            public DateTime DateTime { get; set; }
            public int I { get; set; }
        }
    }
}
