using Microsoft.EntityFrameworkCore;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// AdminChatSessionModuleService：管理后台聊天会话-模块关联服务
    /// </summary>
    public class AdminChatSessionModuleService : BaseClientService<AdminChatSessionModule>
    {
        public AdminChatSessionModuleService(IAdminChatSessionModuleRepository repository, IServiceProvider serviceProvider) 
            : base(repository, serviceProvider)
        {
        }

        /// <summary>
        /// 获取会话关联的所有模块
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        public async Task<List<AdminChatSessionModule>> GetSessionModulesAsync(int sessionId)
        {
            var modules = await base.GetFullListAsync(m => m.SessionId == sessionId, "AddedTime ASC");
            return modules.ToList();
        }

        /// <summary>
        /// 添加模块到会话
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="xncfModuleUid">XNCF 模块 UID</param>
        /// <param name="moduleName">模块名称</param>
        /// <param name="moduleVersion">模块版本</param>
        public async Task<AdminChatSessionModule> AddModuleToSessionAsync(int sessionId, string xncfModuleUid, string moduleName, string moduleVersion)
        {
            // 检查是否已存在（防止重复添加）
            var existing = await base.GetObjectAsync(m => 
                m.SessionId == sessionId && m.XncfModuleUid == xncfModuleUid);

            if (existing != null)
            {
                return existing;
            }

            var module = new AdminChatSessionModule(sessionId, xncfModuleUid, moduleName, moduleVersion);
            await base.SaveObjectAsync(module);
            return module;
        }

        /// <summary>
        /// 批量添加模块到会话
        /// </summary>
        public async Task<List<AdminChatSessionModule>> AddModulesToSessionAsync(
            int sessionId, 
            List<(string uid, string name, string version)> modules)
        {
            var result = new List<AdminChatSessionModule>();

            foreach (var (uid, name, version) in modules)
            {
                var module = await AddModuleToSessionAsync(sessionId, uid, name, version);
                result.Add(module);
            }

            return result;
        }

        /// <summary>
        /// 从会话中移除模块
        /// </summary>
        public async Task<bool> RemoveModuleFromSessionAsync(int sessionId, string xncfModuleUid)
        {
            var module = await base.GetObjectAsync(m => 
                m.SessionId == sessionId && m.XncfModuleUid == xncfModuleUid);

            if (module == null) return false;

            await base.DeleteObjectAsync(module);
            return true;
        }

        /// <summary>
        /// 清空会话的所有模块关联
        /// </summary>
        public async Task<int> ClearSessionModulesAsync(int sessionId)
        {
            var modules = await base.GetFullListAsync(m => m.SessionId == sessionId);

            foreach (var module in modules)
            {
                await base.DeleteObjectAsync(module);
            }

            return modules.Count();
        }

        /// <summary>
        /// 检查模块是否已关联到会话
        /// </summary>
        public async Task<bool> IsModuleInSessionAsync(int sessionId, string xncfModuleUid)
        {
            var module = await base.GetObjectAsync(m => m.SessionId == sessionId && m.XncfModuleUid == xncfModuleUid);
            return module != null;
        }

        /// <summary>
        /// 获取会话的模块数量
        /// </summary>
        public async Task<int> GetSessionModuleCountAsync(int sessionId)
        {
            var modules = await base.GetFullListAsync(m => m.SessionId == sessionId);
            return modules.Count();
        }

        /// <summary>
        /// 获取模块被使用的会话数量（统计用）
        /// </summary>
        public async Task<int> GetModuleUsageCountAsync(string xncfModuleUid)
        {
            var modules = await base.GetFullListAsync(m => m.XncfModuleUid == xncfModuleUid);
            return modules.Select(m => m.SessionId).Distinct().Count();
        }
    }
}
