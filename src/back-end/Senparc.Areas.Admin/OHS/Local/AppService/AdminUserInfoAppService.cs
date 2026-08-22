/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminUserInfoAppService.cs
    文件功能描述：后台管理员用户应用服务
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强后台模块批量更新并完善多语言管理界面

    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

----------------------------------------------------------------*/
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Service;
using Senparc.Ncf.Core.Exceptions;
using System.ComponentModel;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    [BackendJwtAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
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
                    List = list.Select(z =>
                    {
                        var item = _adminUserInfoService.Mapper.Map<AdminUserInfoDto>(z);
                        item.Password = "";
                        return item;
                    }).ToList(),
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


        [FunctionRender("设置数字管", "设置数字管显示", typeof(Register))]
        public async Task<AppResponseBase<AdminUserInfo_SetDigitalPipeResponse>> SetDigitalPipeAsync(AdminUserInfo_SetDigitalPipeRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminUserInfo_SetDigitalPipeResponse>, AdminUserInfo_SetDigitalPipeResponse>(async (response, logger) =>
            {
                try{
                logger.Append("设置数字管：" + request.Number);

                var jsonBody = new { number = request.Number }.ToJson();
                logger.Append("数字管请求 JSON：" + jsonBody);

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
                var result = await Senparc.CO2NET.HttpUtility.Post.PostGetJsonAsync<AdminUserInfo_SetDigitalPipeResponse>(
                    ServiceProvider,
                    "http://192.168.1.175:5000/api/DisplayNumber",
                    cookieContainer: null,
                    fileStream: stream,
                    encoding: Encoding.UTF8,
                    contentType: "application/json");

                logger.Append("数字管接口返回：" + result.ToJson());
                return result;
                }
                catch(Exception ex){
                    logger.Append("设置数字管失败：" + ex.Message);
                    logger.Append("设置数字管失败：" + ex.StackTrace);
                    return new AdminUserInfo_SetDigitalPipeResponse()
                    {
                        Success = false,
                        Data = ex.Message
                    };
                }
            });
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
            //int adminUserInfoId = GetCurrentAdminUserInfoId();
            var response = await this.GetResponseAsync<AppResponseBase<AdminUserInfo_GetRolesResponse>, AdminUserInfo_GetRolesResponse>(async (response, logger) =>
            {
                var roles = await ServiceProvider.GetService<SysRoleAdminUserInfoService>().GetFullListAsync(o => o.AccountId == request.AccountId);
                return new AdminUserInfo_GetRolesResponse()
                {
                    RoleIds = roles.Select(o => o.RoleId)
                };
            });
            return response;
        }

        /// <summary>
        /// 删除管理员
        /// </summary>
        /// <param name="id">管理员 ID</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> DeleteAsync(int id)
        {
            var response = await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                int adminUserInfoId = GetCurrentAdminUserInfoId();

                if (id == adminUserInfoId)
                {
                    throw new NcfExceptionBase("管理员不能删除自己！");
                }

                var adminUserInfo = await _adminUserInfoService.GetObjectAsync(z => z.Id == id);
                if (adminUserInfo == null)
                {
                    throw new NcfExceptionBase("管理员不存在！");
                }

                //TODO：进行更多层级判断

                await _adminUserInfoService.DeleteObjectAsync(adminUserInfo);

                return "删除成功！";
            });
            return response;
        }
    }

    public class AdminUserInfo_SetDigitalPipeRequest:AppRequestBase
    {
        [Description("数字管显示内容")]
        public string Number{get;set;}
    }

    public class AdminUserInfo_SetDigitalPipeResponse
    {
        [Description("是否成功")]
        public bool Success{get;set;}
        [Description("返回数据")]
        public string Data{get;set;}
    }
}
