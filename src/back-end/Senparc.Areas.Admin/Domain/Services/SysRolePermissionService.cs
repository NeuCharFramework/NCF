using Senparc.Areas.Admin.ACL.Repository;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;

namespace Senparc.Areas.Admin.Domain.Services
{
    public class SysRolePermissionService : BaseClientService<SysRolePermission>
    {
        private readonly ISysRolePermissionRepository _repo;

        public SysRolePermissionService(ISysRolePermissionRepository repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            _repo = repo;
        }

        /// <summary>
        /// 检查当前用户是否权限
        /// </summary>
        /// <param name="resourceCodes">资源codes</param>
        /// <param name="adminUserInfoId">当前用户Id</param>
        /// <returns></returns>
        internal async Task<bool> HasPermissionAsync(IEnumerable<string> resourceCodes, int adminUserInfoId)
        {
            if (!resourceCodes.Any())
            {
                return false;
            }
            string cacheKey = string.Format("adminUserInfoPermission:{0}", adminUserInfoId); // 缓存当前用户的所有资源
            var distributedCache = _serviceProvider.GetService<IDistributedCache>();
            string codesJsonValue = await distributedCache.GetStringAsync(cacheKey); // 尝试从缓存读取用户的资源
            IEnumerable<string> codes = null;
            if (string.IsNullOrEmpty(codesJsonValue))
            {
                codes = await _repo.GetAllResouceCodesByAccountIdAsync(adminUserInfoId);
                codesJsonValue = Newtonsoft.Json.JsonConvert.SerializeObject(codes);
                await distributedCache.SetStringAsync(cacheKey, codesJsonValue, new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromHours(8)
                }); // 缓存 8 小时
            }
            else
            {
                codes = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<string>>(codesJsonValue);
            }
            if (codes.Any())
            {
                return false;
            }
            //获取当前用户的所有资源code
            return codes.Intersect(resourceCodes).Any();
        }
    }
}
