using Senparc.Areas.Admin.Domain.Dto;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
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
        /// 获取分页列表
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
        /// 创建or修改角色信息
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="Ncf.Core.Exceptions.NcfExceptionBase"></exception>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        //[Permission(Codes = new string[] { "code" })]
        [Ncf.Core.Authorization.Permission("role.add,role.update")]
        public async Task<AppResponseBase<SystemConfigDto>> CreateOrUpdateAsync(SystemConfig_CreateOrUpdateDto request)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SystemConfigDto>, SystemConfigDto>(async (response, logger) =>
            {
                var systemConfig = await _systemConfigService.GetObjectAsync(z => z.Id == request.Id);

                if (systemConfig == null)
                {
                    throw new NcfExceptionBase("系统配置信息不存在");
                }

                systemConfig = _systemConfigService.Mapper.Map(request, systemConfig);

                await _systemConfigService.SaveObjectAsync(systemConfig);

                var systemConfigDto = _systemConfigService.Mapper.Map<SystemConfigDto>(systemConfig);
                
                return systemConfigDto;
            });
            return response;
        }
    }
}
