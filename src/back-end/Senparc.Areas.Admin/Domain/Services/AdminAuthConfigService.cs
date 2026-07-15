/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminAuthConfigService.cs
    文件功能描述：AdminAuthConfigService 相关功能实现
    
    
    创建标识：Senparc - 20260705
    
    修改标识：Senparc - 20260705
    修改描述：v0.0.3 新增登录超时配置并补齐多数据库迁移支持

    修改标识：Senparc - 20260705
    修改描述：v0.0.4 新增登录超时配置并补齐多数据库迁移支持
----------------------------------------------------------------*/

using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    public class AdminAuthExpireSettings
    {
        public int AdminWebLoginExpireMinutes { get; set; }
        public int BackendJwtExpireMinutes { get; set; }
        public bool UsingDefault { get; set; }
        public string Source { get; set; }
    }

    public class AdminAuthConfigService : BaseClientService<AdminAuthConfig>
    {
        public const int MinExpireMinutes = 5;
        public const int MaxExpireMinutes = 60 * 24 * 365; // 1 year

        public AdminAuthConfigService(IAdminAuthConfigRepository repository, IServiceProvider serviceProvider)
            : base(repository, serviceProvider)
        {
        }

        private static bool IsTableMissingException(Exception ex)
        {
            var message = ex?.ToString() ?? string.Empty;
            var keywords = new[]
            {
                "no such table",
                "invalid object name",
                "doesn't exist",
                "does not exist",
                "relation",
                "ora-00942",
                "对象名无效",
                "表或视图不存在",
                "找不到对象",
                "表不存在"
            };
            return keywords.Any(k => message.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public int NormalizeExpireMinutes(int expireMinutes, int defaultValue)
        {
            var value = expireMinutes <= 0 ? defaultValue : expireMinutes;
            if (value < MinExpireMinutes)
            {
                return MinExpireMinutes;
            }

            if (value > MaxExpireMinutes)
            {
                return MaxExpireMinutes;
            }

            return value;
        }

        public AdminAuthExpireSettings GetEffectiveExpireSettings()
        {
            try
            {
                var config = GetObject(z => true);
                if (config == null)
                {
                    return GetDefaultSettings("missing-record");
                }

                return new AdminAuthExpireSettings
                {
                    AdminWebLoginExpireMinutes = NormalizeExpireMinutes(config.AdminWebLoginExpireMinutes, AdminAuthConfig.DefaultAdminWebLoginExpireMinutes),
                    BackendJwtExpireMinutes = NormalizeExpireMinutes(config.BackendJwtExpireMinutes, AdminAuthConfig.DefaultBackendJwtExpireMinutes),
                    UsingDefault = false,
                    Source = "table"
                };
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                return GetDefaultSettings("missing-table");
            }
            catch
            {
                // 为避免登录流程因为配置表异常中断，这里兜底默认值。
                return GetDefaultSettings("fallback-on-exception");
            }
        }

        private static AdminAuthExpireSettings GetDefaultSettings(string source)
        {
            return new AdminAuthExpireSettings
            {
                AdminWebLoginExpireMinutes = AdminAuthConfig.DefaultAdminWebLoginExpireMinutes,
                BackendJwtExpireMinutes = AdminAuthConfig.DefaultBackendJwtExpireMinutes,
                UsingDefault = true,
                Source = source
            };
        }

        public async Task<(bool Success, string Message, AdminAuthExpireSettings Settings)> TrySaveExpireSettingsAsync(
            int adminWebLoginExpireMinutes,
            int backendJwtExpireMinutes)
        {
            var normalizedWeb = NormalizeExpireMinutes(adminWebLoginExpireMinutes, AdminAuthConfig.DefaultAdminWebLoginExpireMinutes);
            var normalizedJwt = NormalizeExpireMinutes(backendJwtExpireMinutes, AdminAuthConfig.DefaultBackendJwtExpireMinutes);

            AdminAuthConfig config;
            try
            {
                config = await GetObjectAsync(z => true).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                return (false,
                    $"认证配置表 {Register.DATABASE_PREFIX}{nameof(AdminAuthConfig)} 不存在，请先完成数据库升级（EF Core Merge）。",
                    GetDefaultSettings("missing-table"));
            }

            if (config == null)
            {
                config = new AdminAuthConfig(normalizedWeb, normalizedJwt);
            }
            else
            {
                config.Update(normalizedWeb, normalizedJwt);
            }

            await SaveObjectAsync(config).ConfigureAwait(false);

            return (true,
                $"已保存：网页登录持续时间={normalizedWeb} 分钟，JWT 过期时间={normalizedJwt} 分钟。",
                new AdminAuthExpireSettings
                {
                    AdminWebLoginExpireMinutes = normalizedWeb,
                    BackendJwtExpireMinutes = normalizedJwt,
                    UsingDefault = false,
                    Source = "table"
                });
        }
    }
}
