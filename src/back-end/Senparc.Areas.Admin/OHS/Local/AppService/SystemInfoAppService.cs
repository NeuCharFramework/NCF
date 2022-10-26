using Senparc.Areas.Admin.Domain.Dto;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;
using Senparc.Xncf.SystemManager.Domain.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    [BackendJwtAuthorize]
    public class SystemInfoAppService : LocalAppServiceBase
    {
        private readonly SystemConfigService _systemConfigService;
        private readonly FullSystemConfigCache _fullSystemConfigCache;
        public SystemInfoAppService(IServiceProvider serviceProvider, SystemConfigService systemConfigService, FullSystemConfigCache fullSystemConfigCache) : base(serviceProvider)
        {
            _systemConfigService = systemConfigService;
            _fullSystemConfigCache = fullSystemConfigCache;
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
    }
}
