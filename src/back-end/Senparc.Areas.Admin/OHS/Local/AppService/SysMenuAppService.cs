using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    /// <summary>
    /// 菜单表
    /// </summary>
    [BackendJwtAuthorize]
    public class SysMenuAppService : AppServiceBase
    {
        private readonly SysMenuService _sysMenuService;
        private readonly Domain.Services.SysMenuService _domainService;
        private readonly AutoMapper.IMapper _mapper;

        public SysMenuAppService(IServiceProvider serviceProvider, SysMenuService sysMenuService, AutoMapper.IMapper mapper, Domain.Services.SysMenuService domainService) : base(serviceProvider)
        {
            _sysMenuService = sysMenuService;
            _mapper = mapper;
            _domainService = domainService;
        }

        /// <summary>
        /// 创建菜单
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<AppResponseBase<SysMenu_CreateOrUpdateResponse>> CreateOrUpdateAsync(SysMenu_CreateOrUpdateRequest request)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysMenu_CreateOrUpdateResponse>, SysMenu_CreateOrUpdateResponse>(async (response, logger) =>
            {
                var dto = _mapper.Map<SysMenuDto>(request);
                var menu = await _sysMenuService.CreateOrUpdateAsync(dto);
                return new SysMenu_CreateOrUpdateResponse()
                {
                    Id = menu.Id
                };
            });
            return response;
        }

        /// <summary>
        /// 获取菜单树 不包含按钮
        /// </summary>
        /// <param name="hasButton">是否包含按钮</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        public async Task<AppResponseBase<SysMenu_MenuTreeResponse>> GetAllMenusTreeAsync(bool hasButton)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysMenu_MenuTreeResponse>, SysMenu_MenuTreeResponse>(async (response, logger) =>
            {
                var mm = await _domainService.GetAllMenusTreeAsync(hasButton);
                return new SysMenu_MenuTreeResponse()
                {
                    Items = mm
                };
            });
            return response;
        }

        /// <summary>
        /// 获取菜单列表
        /// </summary>
        /// <param name="parentId">父级Id</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        public async Task<AppResponseBase<SysMenu_MenusResponse>> GetMenusAsync(string parentId)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysMenu_MenusResponse>, SysMenu_MenusResponse>(async (response, logger) =>
            {
                SenparcExpressionHelper<SysMenu> helper = new SenparcExpressionHelper<SysMenu>();
                helper
                .ValueCompare
                .AndAlso(string.IsNullOrEmpty(parentId), o => o.ParentId == null || o.ParentId == string.Empty)
                .AndAlso(!string.IsNullOrEmpty(parentId), o => o.ParentId == parentId)
                ;
                var ms = await _domainService.GetFullListAsync(helper.BuildWhereExpression(), "Sort DESC");
                var items = _mapper.Map<IEnumerable<SysMenuDto>>(ms.AsEnumerable());
                return new SysMenu_MenusResponse()
                {
                    Items = items
                };
            });
            return response;
        }

        /// <summary>
        /// 获取菜单详情
        /// </summary>
        /// <param name="id">父级Id</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        public async Task<AppResponseBase<SysMenu_GetMenuResponse>> GetMenuAsync(string id)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysMenu_GetMenuResponse>, SysMenu_GetMenuResponse>(async (response, logger) =>
            {
                var ms = await _domainService.GetObjectAsync(o => o.Id == id);
                return new SysMenu_GetMenuResponse()
                {
                    Item = _mapper.Map<SysMenuDto>(ms)
                };
            });
            return response;
        }

        /// <summary>
        /// 删除菜单
        /// </summary>
        /// <param name="id">父级Id</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Delete)]
        public async Task<AppResponseBase<SysMenu_GetMenuResponse>> DeleteMenuAsync(string id)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysMenu_GetMenuResponse>, SysMenu_GetMenuResponse>(async (response, logger) =>
            {
                var ms = await _domainService.GetObjectAsync(o => o.Id == id);
                await _domainService.DeleteObjectAsync(ms);
                return new SysMenu_GetMenuResponse()
                {
                    Item = _mapper.Map<SysMenuDto>(ms)
                };
            });
            return response;
        }
    }
}
