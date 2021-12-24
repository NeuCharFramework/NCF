using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Models.Dto;
using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.Areas.Admin.OHS.PL;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    [BackendJwtAuthorize]
    public class AdminUserInfoAppService : LocalAppServiceBase
    {
        private readonly AdminUserInfoService _adminUserInfoService;
        private readonly AutoMapper.IMapper _mapper;
        public AdminUserInfoAppService(IServiceProvider serviceProvider, AutoMapper.IMapper mapper, AdminUserInfoService adminUserInfoService) : base(serviceProvider)
        {
            this._adminUserInfoService = adminUserInfoService;
            _mapper = mapper;
        }

        [ApiBind]
        public async Task<AppResponseBase<AdminUserInfo_GetListResponse>> GetList(int pageIndex, int pageSize)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminUserInfo_GetListResponse>, AdminUserInfo_GetListResponse>(async (response, logger) =>
            {
                var list = await _adminUserInfoService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                return new AdminUserInfo_GetListResponse()
                {
                    List = list.Select(z => _adminUserInfoService.Mapper.Map<AdminUserInfoDto>(z)).ToList(),
                    TotalCount = list.TotalCount
                };
            });
        }

        /// <summary>
        /// 创建管理员
        /// </summary>
        /// <param name="request">管理员创建请求</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AdminUserInfo_CreateResponse>> Create(AdminUserInfo_CreateOrUpdateRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminUserInfo_CreateResponse>, AdminUserInfo_CreateResponse>(async (response, logger) =>
            {
                var dto = _mapper.Map<CreateOrUpdate_AdminUserInfoDto>(request);
                var adminUserInfo = await _adminUserInfoService.CreateAdminUserInfoAsync(dto);
                return new AdminUserInfo_CreateResponse()
                {
                    AdminUserInfoId = adminUserInfo.Id
                };
            });
        }

        /// <summary>
        /// 修改管理员
        /// </summary>
        /// <param name="request">管理员创建请求</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Put)]
        public async Task<AppResponseBase<AdminUserInfo_CreateResponse>> Update(AdminUserInfo_CreateOrUpdateRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminUserInfo_CreateResponse>, AdminUserInfo_CreateResponse>(async (response, logger) =>
            {
                var dto = _mapper.Map<CreateOrUpdate_AdminUserInfoDto>(request);
                var adminUserInfo = await _adminUserInfoService.UpdateAdminUserInfoAsync(dto);
                return new AdminUserInfo_CreateResponse()
                {
                    AdminUserInfoId = adminUserInfo.Id
                };
            });
        }

        /// <summary>
        /// 管理员登录验证，并进行登录授权
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        [FunctionRender("管理员登录", "测试当前管理员登录", typeof(Register))]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<AppResponseBase<AccountLoginResultDto>> LoginAsync([FromBody] AdminUserInfo_LoginRequest request)
        {
            AppResponseBase<AccountLoginResultDto> resultDto = await this.GetResponseAsync<AppResponseBase<AccountLoginResultDto>, AccountLoginResultDto>(async (response, logger) =>
            {
                var result = await _adminUserInfoService.LoginAsync(new AccountLoginDto()
                {
                    UserName = request.UserName,
                    Password = request.Password
                });
                logger.Append("管理员登录：" + request.UserName);
                logger.Append("结果：" + !result.UserName.IsNullOrEmpty());

                return result;
            }, exceptionHandler: (ex, response, logger) =>
            {
                logger.Append(ex.Message);
                logger.Append(ex.StackTrace);

                response.ErrorMessage = ex.Message;
            },
            saveLogAfterFinished: true,
            saveLogName: "管理员登录");
            return resultDto;
        }

        /// <summary>
        /// 获取当前管理员信息
        /// </summary>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        public async Task<AppResponseBase<AccountLoginResultDto>> GetAdminUserInfoAsync()
        {
            AppResponseBase<AccountLoginResultDto> resultDto = await this.GetResponseAsync<AppResponseBase<AccountLoginResultDto>, AccountLoginResultDto>(async (response, logger) =>
            {
                var result = await _adminUserInfoService.GetAdminUserInfoAsync(GetCurrentAdminUserInfoId());
                return result;
            });
            return resultDto;
        }
        

        /// <summary>
        /// 增加角色
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Put)]
        public async Task<AppResponseBase<AdminUserInfo_AddRoleResponse>> AddRoleAsync(AdminUserInfo_AddRoleRequest request)
        {
            var response = await this.GetResponseAsync<AppResponseBase<AdminUserInfo_AddRoleResponse>, AdminUserInfo_AddRoleResponse>(async (response, logger) =>
            {
                await ServiceProvider.GetService<SysRoleAdminUserInfoService>().AddAsync(request.RoleId, request.AccountId);
                return new AdminUserInfo_AddRoleResponse();
            });
            return response;
        }

        /// <summary>
        /// 获取当前用户的所有角色
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Put)]
        public async Task<AppResponseBase<AdminUserInfo_GetRolesResponse>> GetRolesAsync(AdminUserInfo_AddRoleRequest request)
        {
            int adminUserInfoId = GetCurrentAdminUserInfoId();
            var response = await this.GetResponseAsync<AppResponseBase<AdminUserInfo_GetRolesResponse>, AdminUserInfo_GetRolesResponse>(async (response, logger) =>
            {
                var roles = await ServiceProvider.GetService<SysRoleAdminUserInfoService>().GetFullListAsync(o => o.AccountId == adminUserInfoId);
                return new AdminUserInfo_GetRolesResponse()
                {
                    RoleIds = roles.Select(o => o.RoleId)
                };
            });
            return response;
        }
    }
}
