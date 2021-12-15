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

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    public class AdminUserInfoAppService : AppServiceBase
    {
        private readonly AdminUserInfoService _adminUserInfoService;
        private readonly AutoMapper.IMapper _mapper;
        public AdminUserInfoAppService(IServiceProvider serviceProvider, AutoMapper.IMapper mapper, AdminUserInfoService adminUserInfoService) : base(serviceProvider)
        {
            this._adminUserInfoService = adminUserInfoService;
            _mapper = mapper;
        }

        [ApiBind]
        [BackendJwtAuthorize]
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
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post, BaseApiControllerType = typeof(TempApiBaseController))]
        [BackendJwtAuthorize]
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
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Put, BaseApiControllerType = typeof(TempApiBaseController))]
        [BackendJwtAuthorize]
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
        public async Task<object> LoginAsync([FromBody] AdminUserInfo_LoginRequest request)
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
    }

    /// <summary>
    /// 临时生成API控制器的基类
    /// [ApiController] 可以 自动 验证模型状态 模型验证不通过则
    /// </summary>
    [ApiController]
    public class TempApiBaseController: ControllerBase
    {

    }
}
