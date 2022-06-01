using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    [BackendJwtAuthorize]
    public class SysRoleAppService : AppServiceBase
    {
        private readonly SysRoleService _sysRoleService;
        private readonly SysPermissionService _sysPermissionService;
        private readonly AutoMapper.IMapper _mapper;

        public SysRoleAppService(SysRoleService sysRoleService, IServiceProvider serviceProvider, AutoMapper.IMapper mapper, SysPermissionService sysPermissionService) : base(serviceProvider)
        {
            _sysRoleService = sysRoleService;
            _mapper = mapper;
            _sysPermissionService = sysPermissionService;
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
        public async Task<AppResponseBase<SysRole_CreateOrUpdateResponse>> CreateOrUpdateAsync(SysRole_CreateOrUpdateRequest request)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysRole_CreateOrUpdateResponse>, SysRole_CreateOrUpdateResponse>(async (response, logger) =>
            {
                var dto = _mapper.Map<SysRoleDto>(request);
                var codeRepeat = await _sysRoleService.GetCountAsync(o => o.RoleCode == request.RoleCode && o.Id != request.Id);
                if (codeRepeat > 0)
                {
                    throw new Ncf.Core.Exceptions.NcfExceptionBase($"{request.RoleCode} 重复");
                }
                await _sysRoleService.CreateOrUpdateAsync(dto);
                var modifyRole = await _sysRoleService.GetObjectAsync(o => o.RoleCode == dto.RoleCode);
                return new SysRole_CreateOrUpdateResponse()
                {
                    Id = modifyRole.Id
                };
            });
            return response;
        }

        /// <summary>
        /// 获取分页列表
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        //[Permission("role.query")]
        public async Task<AppResponseBase<SysRole_GetListResponse>> GetListAsync([FromQuery] SysRole_GetListRequest request)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysRole_GetListResponse>, SysRole_GetListResponse>(async (response, logger) =>
            {
                SenparcExpressionHelper<SysRole> helper = new SenparcExpressionHelper<SysRole>();
                var list = await _sysRoleService.GetObjectListAsync(request.PageIndex, request.PageSize, helper.BuildWhereExpression(), _ => _.AddTime, Ncf.Core.Enums.OrderingType.Descending, null);
                var dtoItems = _mapper.Map<IEnumerable<Domain.Dto.SysRoleListDto>>(list.AsEnumerable());
                return new SysRole_GetListResponse()
                {
                    TotalCount = list.TotalCount,
                    List = dtoItems
                };
            });
            return response;
        }

        /// <summary>
        /// 获取详情
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        //[Permission("role.detail")]
        public async Task<AppResponseBase<SysRole_GetSingleResponse>> GetSingeAsync(string id)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysRole_GetSingleResponse>, SysRole_GetSingleResponse>(async (response, logger) =>
            {
                SenparcExpressionHelper<SysRole> helper = new SenparcExpressionHelper<SysRole>();
                var entity = await _sysRoleService.GetObjectAsync(o => o.Id == id);
                var item = _mapper.Map<Domain.Dto.SysRoleDetailDto>(entity);
                return new SysRole_GetSingleResponse()
                {
                    Detail = item
                };
            });
            return response;
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        //[Permission("role.delete")]
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Delete)]
        public async Task<AppResponseBase<SysRole_GetSingleResponse>> DeleteSingeAsync(string id)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysRole_GetSingleResponse>, SysRole_GetSingleResponse>(async (response, logger) =>
            {
                SenparcExpressionHelper<SysRole> helper = new SenparcExpressionHelper<SysRole>();
                var entity = await _sysRoleService.GetObjectAsync(o => o.Id == id);
                await _sysRoleService.DeleteObjectAsync(entity);
                var item = _mapper.Map<Domain.Dto.SysRoleDetailDto>(entity);
                return new SysRole_GetSingleResponse()
                {
                    Detail = item
                };
            });
            return response;
        }

        /// <summary>
        /// 增加权限
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Put)]
        //[Permission("role.addPermission")]
        public async Task<AppResponseBase<SysRole_Response>> AddPermissionAsync(SysRole_AddPermissionRequest request)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysRole_Response>, SysRole_Response>(async (response, logger) =>
            {
                SenparcExpressionHelper<SysRole> helper = new SenparcExpressionHelper<SysRole>();
                var dto = _mapper.Map<IEnumerable<SysPermissionDto>>(request.RequestDtos);
                await _sysPermissionService.AddAsync(dto);
                return new SysRole_Response()
                {
                };
            });
            return response;
        }

        /// <summary>
        /// 获取角色下面的所有菜单
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        //[Permission("role.getPermission")]
        public async Task<AppResponseBase<SysRole_PermissionsResponse>> GetRolePermissionsAsync(string roleId)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysRole_PermissionsResponse>, SysRole_PermissionsResponse>(async (response, logger) =>
            {
                var data = await _sysPermissionService.GetFullListAsync(_ => _.RoleId == roleId);
                return new SysRole_PermissionsResponse()
                {
                    PermissionId = data.Select(x => x.PermissionId).ToArray()
                };
            });
            return response;
        }
    }
}
