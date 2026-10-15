/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Chat.cshtml.cs
    文件功能描述：Chat.cshtml.cs implementation and project behavior.


    创建标识：Senparc - 20260325

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Pages.AdminChat
{
    /// <summary>
    /// 与 SenparcTrace/Index 等页一致：<see cref="Ncf.AreaBase.Admin.Filters.IgnoreAuth"/> 跳过菜单 URL 校验；
    /// 登录与 AdminOnly 由 <see cref="BaseAdminPageModel"/> 与 Cookie 中间件统一处理，不在此页写 Login 跳转。
    /// </summary>
    [Ncf.AreaBase.Admin.Filters.IgnoreAuth]
    public class ChatModel : BaseAdminPageModel
    {
        private readonly AdminChatSessionService _sessionService;
        private readonly IAdminWorkContextProvider _adminWorkContextProvider;

        public ChatModel(
            IServiceProvider serviceProvider,
            AdminChatSessionService sessionService,
            IAdminWorkContextProvider adminWorkContextProvider)
            : base(serviceProvider)
        {
            _sessionService = sessionService;
            _adminWorkContextProvider = adminWorkContextProvider;
        }

        /// <summary>
        /// 会话ID（URL参数）
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int SessionId { get; set; }

        /// <summary>
        /// 初始消息（URL参数，可选）
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string InitialMessage { get; set; }

        /// <summary>
        /// 当前用户ID
        /// </summary>
        public int CurrentUserId { get; set; }

        /// <summary>
        /// 当前用户是否为超级管理员（administrator 角色）：超级管理员可打开用量统计面板
        /// </summary>
        public bool IsSuperAdmin { get; set; }

        /// <summary>
        /// 模块 UID 列表（逗号分隔的字符串）
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string ModuleUids { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var adminWorkContext = _adminWorkContextProvider.GetAdminWorkContext();
            CurrentUserId = adminWorkContext.AdminUserId;
            IsSuperAdmin = (adminWorkContext.RoleCodes ?? Enumerable.Empty<string>())
                .Any(role => string.Equals(role?.Trim(), Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, System.StringComparison.OrdinalIgnoreCase));

            if (SessionId > 0)
            {
                var session = await _sessionService.GetSessionByIdAsync(SessionId, CurrentUserId);
                if (session == null)
                {
                    return RedirectToPage("../Index");
                }
            }

            return Page();
        }
    }
}
