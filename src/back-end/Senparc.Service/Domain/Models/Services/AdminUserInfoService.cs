using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Senparc.Core.Models;
using Senparc.Repository;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.Log;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Senparc.Core;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Senparc.CO2NET;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Service;

namespace Senparc.Service
{
    public class AdminUserInfoService : BaseClientService<AdminUserInfo>
    {

        private readonly Lazy<IHttpContextAccessor> _contextAccessor;
        public AdminUserInfoService(AdminUserInfoRepository repository, Lazy<IHttpContextAccessor> httpContextAccessor, IServiceProvider serviceProvider)
            : base(repository, serviceProvider)
        {
            _contextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool CheckUserNameExisted(long id, string userName)
        {
            userName = userName.Trim().ToUpper();

            return
            GetObject(
                z => z.Id != id && z.UserName.ToUpper() == userName /*z.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase)*/) != null;
        }

        [ApiBind]
        [Core.BackendJwtAuthorize]
        public async Task<AdminUserInfo> GetUserInfo(string userName)
        {
            await Task.CompletedTask;
            var obj = GetObject(z => z.UserName.Equals(userName.Trim()));
            return obj;
        }

        public AdminUserInfo GetUserInfo(string userName, string password)
        {
            AdminUserInfo userInfo = GetObject(z => z.UserName.Equals(userName), null);
            if (userInfo == null)
            {
                return null;
            }
            var codedPassword = GetPassword(password, userInfo.PasswordSalt, false);
            return userInfo.Password == codedPassword ? userInfo : null;
        }

        public string GetPassword(string password, string salt, bool isMD5Password)
        {
            string md5 = password.ToUpper().Replace("-", "");
            if (!isMD5Password)
            {
                md5 = MD5.GetMD5Code(password, "").Replace("-", ""); //原始MD5
            }
            return MD5.GetMD5Code(md5, salt).Replace("-", ""); //再加密
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _contextAccessor.Value.HttpContext.SignOutAsync(SiteConfig.NcfAdminAuthorizeScheme);
            }
            catch (Exception ex)
            {
                LogUtility.AdminUserInfo.ErrorFormat("退出登录失败。", ex);
            }
        }

        public virtual void Login(AdminUserInfo userInfo, bool rememberMe)
        {
            #region 使用 .net core 的方法写入 cookie 验证信息

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userInfo.UserName),
                new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString(), ClaimValueTypes.Integer),
                new Claim("AdminMember", "", ClaimValueTypes.String)
            };
            var identity = new ClaimsIdentity(SiteConfig.NcfAdminAuthorizeScheme);
            identity.AddClaims(claims);
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(120),
                IsPersistent = false,
            };

            LogoutAsync().ConfigureAwait(false).GetAwaiter().GetResult(); //退出登录
            _contextAccessor.Value.HttpContext.SignInAsync(SiteConfig.NcfAdminAuthorizeScheme, new ClaimsPrincipal(identity), authProperties);

            #endregion
        }

        public bool CheckPassword(string userName, string password)
        {
            var userInfo = GetObject(z => z.UserName.Equals(userName));
            if (userInfo == null)
            {
                return false;
            }
            var codedPassword = GetPassword(password, userInfo.PasswordSalt, false);
            return userInfo.Password == codedPassword;
        }

        public AdminUserInfo TryLogin(string userNameOrEmail, string password, bool rememberMe)
        {
            AdminUserInfo userInfo = GetUserInfo(userNameOrEmail, password);
            if (userInfo != null)
            {
                Login(userInfo, rememberMe);
                return userInfo;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 添加用户信息
        /// </summary>
        /// <param name="objDto"></param>
        public void CreateAdminUserInfo(CreateOrUpdate_AdminUserInfoDto objDto)
        {
            string userName = objDto.UserName;
            string password = objDto.Password;
            var obj = new AdminUserInfo(ref userName, ref password, null, null, objDto.Note);
            SaveObject(obj);
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="objDto"></param>
        public void UpdateAdminUserInfo(CreateOrUpdate_AdminUserInfoDto objDto)
        {
            var obj = this.GetObject(z => z.Id == objDto.Id);
            if (obj == null)
            {
                throw new Exception("用户信息不存在！");
            }
            obj.UpdateObject(objDto);
            SaveObject(obj);
        }

        public CreateOrUpdate_AdminUserInfoDto GetAdminUserInfo(int id,params string[] includes)
        {
            var obj = GetObject(z => z.Id == id, includes: includes);

            var objDto = base.Mapper.Map<CreateOrUpdate_AdminUserInfoDto>(obj);

            return objDto;
        }

        public List<AdminUserInfo> GetAdminUserInfo(List<int> ids,params string[] includes)
        {
            return GetFullList(z => ids.Contains(z.Id), z => z.Id, Ncf.Core.Enums.OrderingType.Ascending, includes: includes);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public AdminUserInfo Init(out string userName, out string password)
        {
            userName = null;
            password = null;

            var oldAdminUserInfo = GetObject(z => true);
            if (oldAdminUserInfo != null)
            {
                return null;
            }
            var adminUserInfo = new AdminUserInfo(ref userName, ref password, null, null, "初始化数据");
            SaveObject(adminUserInfo);
            return adminUserInfo;
        }

        public void DeleteObject(int id)
        {
            var obj = GetObject(z => z.Id == id);
            if (obj == null)
            {
                throw new Exception("用户信息不存在！");
            }
            DeleteObject(obj);
        }

        public override void SaveObject(AdminUserInfo obj)
        {
            var isInsert = base.IsInsert(obj);
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("AdminUserInfo{2}：{0}（ID：{1}）", obj.UserName, obj.Id, isInsert ? "新增" : "编辑");
        }

        public override void DeleteObject(AdminUserInfo obj)
        {
            base.DeleteObject(obj);
            LogUtility.WebLogger.InfoFormat("AdminUserInfo被删除：{0}（ID：{1}）", obj.UserName, obj.Id);
        }

        /// <summary>
        /// 登录
        /// <paramref name="loginDto">登录dto</paramref>
        /// </summary>
        /// <returns></returns>
        //[ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<Core.Models.DataBaseModel.Dto.AccountLoginResultDto> LoginAsync(Core.Models.DataBaseModel.Dto.AccountLoginDto loginDto)
        {
            Core.Models.DataBaseModel.Dto.AccountLoginResultDto result = new Core.Models.DataBaseModel.Dto.AccountLoginResultDto();
            string token;
            var userInfo = await GetObjectAsync(z => z.UserName == loginDto.UserName);
            if (userInfo == null)
            {
                throw new Ncf.Core.Exceptions.NcfExceptionBase($"用户名不存在或密码不正确：{loginDto.UserName}（101）！");
            }
            var adminUserInfo = TryLogin(loginDto.UserName, loginDto.Password, false);
            if (adminUserInfo == null)
            {
                throw new Ncf.Core.Exceptions.NcfExceptionBase("用户名不存在或密码不正确（102）！");
            }
            else
            {
                token = GenerateToken(adminUserInfo.Id);
            }

            var roleCodes = await BaseData.BaseDB.BaseDataContext.Set<Ncf.Core.Models.DataBaseModel.SysRoleAdminUserInfo>().Where(o => o.AccountId == adminUserInfo.Id)
                .Select(o => o.RoleCode).Distinct()
                .ToListAsync();
            result.Token = token;
            result.UserName = adminUserInfo.UserName;
            result.RoleCodes = roleCodes;
            return result;
        }


        /// <summary>
        /// GenerateToken
        /// </summary>
        /// <param name="memberId"></param>
        /// <returns></returns>
        private string GenerateToken(int memberId)
        {
            var options = _serviceProvider.GetService<IOptionsSnapshot<JwtSettings>>();
            var jwtSettings = options.Get(JwtSettings.Position_Backend);
            byte[] keyBytes = System.Text.Encoding.ASCII.GetBytes(jwtSettings.SecretKey);
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor securityToken = new SecurityTokenDescriptor()
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, memberId.ToString(), ClaimValueTypes.Integer)
                }),
                Audience = jwtSettings.Audience,
                Issuer = jwtSettings.Issuer,
                Expires = DateTime.UtcNow.AddHours(jwtSettings.Expires),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };
            SecurityToken token = tokenHandler.CreateToken(securityToken);
            string tokenStr = tokenHandler.WriteToken(token);
            return tokenStr;
        }
    }
}
