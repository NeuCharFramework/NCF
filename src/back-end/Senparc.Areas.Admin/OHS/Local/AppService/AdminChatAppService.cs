/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminChatAppService.cs
    文件功能描述：后台 AI 对话应用服务
    
    
    创建标识：Senparc - 20260325
    
    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强后台模块批量更新并完善多语言管理界面

    修改标识：Senparc - 20260726
    修改描述：v0.1.0 增加后台 Admin Chat 同步服务以支持桌面管理交互

    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

    修改标识：Senparc - 20260808
    修改描述：v0.4.0 适配桌面管理员换票相关的后台对话安全上下文

    修改标识：Senparc - 20260822
    修改描述：v0.6.0 新增管理端 Chat 会话工作流能力

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    修改标识：Senparc - 20261009
    修改描述：v0.11.0 增强后台 Admin Chat 群组配置、提示词构建与上下文控制

    修改标识：Senparc - 20261021
    修改描述：v0.11.1 返回最近任务、工作区上下文和会话标题

    修改标识：Senparc - 20261102
    修改描述：提供当前用户 Memory 编辑、会话开关及租户 SystemMessage 设置

    修改标识：Senparc - 20261103
    修改描述：提供同账号跨设备会话排序接口

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 完善后台聊天会话管理与排序

----------------------------------------------------------------*/
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Areas.Admin.OHS.Local.Events;
using Senparc.Xncf.Workspace.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    /// <summary>
    /// AdminChatAppService：管理后台聊天功能 API 服务
    /// 支持 Cookie 和 JWT 两种认证方式
    /// </summary>
    [AdminOrJwtAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class AdminChatAppService : LocalAppServiceBase
    {
        private const string AgentsManagerModuleUid = "D858D7FA-775A-4690-9023-CFB0B3B84994";

        private readonly AdminChatSessionService _sessionService;
        private readonly AdminChatMessageService _messageService;
        private readonly AdminChatSessionModuleService _sessionModuleService;
        private readonly AdminChatSessionWorkflowService _sessionWorkflowService;
        private readonly AdminChatTrajectoryService _trajectoryService;
        private readonly AdminChatAiService _chatAiService;
        private readonly AdminUserInfoService _adminUserInfoService;
        private readonly IStringLocalizer<AdminResource> _localizer;
        private readonly IEventBus _eventBus;
        private readonly IWorkspaceStorage _workspaceStorage;

        public AdminChatAppService(
            IServiceProvider serviceProvider,
            AdminChatSessionService sessionService,
            AdminChatMessageService messageService,
            AdminChatSessionModuleService sessionModuleService,
            AdminChatSessionWorkflowService sessionWorkflowService,
            AdminChatTrajectoryService trajectoryService,
            AdminChatAiService chatAiService,
            AdminUserInfoService adminUserInfoService,
            IStringLocalizer<AdminResource> localizer,
            IWorkspaceStorage workspaceStorage = null) : base(serviceProvider)
        {
            _sessionService = sessionService;
            _messageService = messageService;
            _sessionModuleService = sessionModuleService;
            _sessionWorkflowService = sessionWorkflowService;
            _trajectoryService = trajectoryService;
            _chatAiService = chatAiService;
            _adminUserInfoService = adminUserInfoService;
            _localizer = localizer;
            _eventBus = serviceProvider.GetService<IEventBus>();
            _workspaceStorage = workspaceStorage;
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<AdminChatMemoryView>> GetMemoryAsync(int sessionId = 0)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatMemoryView>, AdminChatMemoryView>(
                async (response, logger) => await ServiceProvider.GetRequiredService<AdminChatMemoryService>()
                    .GetAsync(GetCurrentAdminUserInfoId(), sessionId));
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AdminChatMemoryView>> SaveMemoryAsync([FromBody] SaveAdminChatMemoryRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatMemoryView>, AdminChatMemoryView>(
                async (response, logger) =>
                {
                    if (request == null) throw new NcfExceptionBase("Memory request required.");
                    return await ServiceProvider.GetRequiredService<AdminChatMemoryService>().SaveAsync(
                        GetCurrentAdminUserInfoId(), request.SessionId, request.Revision, request.Markdown, request.SystemMessage);
                });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AdminChatMemoryView>> SetMemoryEnabledAsync([FromBody] SetAdminChatMemoryRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatMemoryView>, AdminChatMemoryView>(
                async (response, logger) =>
                {
                    if (request == null) throw new NcfExceptionBase("Memory request required.");
                    return await ServiceProvider.GetRequiredService<AdminChatMemoryService>().SetModeAsync(
                        GetCurrentAdminUserInfoId(), request.SessionId, request.Revision,
                        request.Mode ?? (request.Enabled ? AdminChatMemoryMode.Learn : AdminChatMemoryMode.Off));
                });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AdminChatMemoryView>> EditMemoryEntryAsync([FromBody] EditAdminChatMemoryRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatMemoryView>, AdminChatMemoryView>(
                async (response, logger) =>
                {
                    if (request == null) throw new NcfExceptionBase("Memory request required.");
                    return await ServiceProvider.GetRequiredService<AdminChatMemoryService>().EditAsync(
                        GetCurrentAdminUserInfoId(), request.SessionId, request.Revision, request.Entry);
                });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<AdminChatSystemMessageView>> GetGlobalChatSystemMessageAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatSystemMessageView>, AdminChatSystemMessageView>(
                async (response, logger) =>
                {
                    if (!IsCurrentAdminSuperAdmin()) throw new NcfExceptionBase("Super administrator required.");
                    var profile = await ServiceProvider.GetRequiredService<AdminChatMemoryService>().GetGlobalAsync();
                    return new AdminChatSystemMessageView(profile.Revision, profile.SystemMessage);
                });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AdminChatSystemMessageView>> SaveGlobalChatSystemMessageAsync(
            [FromBody] AdminChatSystemMessageView request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatSystemMessageView>, AdminChatSystemMessageView>(
                async (response, logger) =>
                {
                    if (!IsCurrentAdminSuperAdmin()) throw new NcfExceptionBase("Super administrator required.");
                    if (request == null) throw new NcfExceptionBase("SystemMessage request required.");
                    var profile = await ServiceProvider.GetRequiredService<AdminChatMemoryService>()
                        .SaveGlobalAsync(request.Revision, request.SystemMessage);
                    logger.Append("Updated tenant-wide AdminChat SystemMessage.");
                    return new AdminChatSystemMessageView(profile.Revision, profile.SystemMessage);
                });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetWorkspaceOptionsResponse>> GetWorkspaceOptionsAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<GetWorkspaceOptionsResponse>, GetWorkspaceOptionsResponse>(async (response, logger) =>
            {
                if (!IsCurrentAdminSuperAdmin() || _workspaceStorage == null)
                {
                    throw new NcfExceptionBase("只有超级管理员可以使用 Workspace。");
                }

                return new GetWorkspaceOptionsResponse
                {
                    Workspaces = (await _workspaceStorage.ListWorkspacesAsync().ConfigureAwait(false))
                        .Select(WorkspaceOptionDto.CreateFromDescriptor)
                        .ToList()
                };
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<WorkspaceOptionDto>> CreateWorkspaceAsync([FromBody] CreateWorkspaceRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<WorkspaceOptionDto>, WorkspaceOptionDto>(async (response, logger) =>
            {
                if (!IsCurrentAdminSuperAdmin() || _workspaceStorage == null)
                {
                    throw new NcfExceptionBase("只有超级管理员可以创建 Workspace。");
                }

                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    throw new NcfExceptionBase("Workspace 名称不能为空。");
                }

                var descriptor = await _workspaceStorage.CreateAsync(
                    new WorkspaceCreateRequest(GetCurrentAdminUserInfoId(), request.Name.Trim())).ConfigureAwait(false);
                return WorkspaceOptionDto.CreateFromDescriptor(descriptor);
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<WorkspaceEntriesResponse>> GetWorkspaceEntriesAsync(
            int sessionId,
            string relativePath = "")
        {
            return await this.GetResponseAsync<AppResponseBase<WorkspaceEntriesResponse>, WorkspaceEntriesResponse>(async (response, logger) =>
            {
                var session = await GetWorkspaceSessionAsync(sessionId).ConfigureAwait(false);
                var entries = await _workspaceStorage.ListAsync(
                    session.WorkspaceId,
                    relativePath ?? string.Empty).ConfigureAwait(false);
                return new WorkspaceEntriesResponse
                {
                    CurrentPath = relativePath ?? string.Empty,
                    Entries = entries
                };
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<WorkspaceFilePreviewDto>> GetWorkspaceFilePreviewAsync(
            int sessionId,
            string relativePath)
        {
            return await this.GetResponseAsync<AppResponseBase<WorkspaceFilePreviewDto>, WorkspaceFilePreviewDto>(async (response, logger) =>
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    throw new NcfExceptionBase("文件路径不能为空。");
                }

                var session = await GetWorkspaceSessionAsync(sessionId).ConfigureAwait(false);
                var file = await _workspaceStorage.ReadFileBytesAsync(
                    session.WorkspaceId,
                    relativePath,
                    2 * 1024 * 1024).ConfigureAwait(false);
                var contentType = ResolveWorkspaceContentType(relativePath);
                var isText = IsWorkspaceTextContent(contentType, relativePath);
                return new WorkspaceFilePreviewDto
                {
                    Path = relativePath,
                    ContentType = contentType,
                    Size = file.Content.LongLength,
                    IsText = isText,
                    Content = isText ? Encoding.UTF8.GetString(file.Content) : null,
                    ContentBase64 = Convert.ToBase64String(file.Content)
                };
            });
        }

        private async Task<AdminChatSession> GetWorkspaceSessionAsync(int sessionId)
        {
            var user = ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext.User;
            return await ServiceProvider.GetRequiredService<AdminChatWorkspaceAssetService>()
                .GetSessionAsync(sessionId, user).ConfigureAwait(false);
        }

        private static string ResolveWorkspaceContentType(string relativePath)
        {
            return Path.GetExtension(relativePath).ToLowerInvariant() switch
            {
                ".html" or ".htm" => "text/html",
                ".css" => "text/css",
                ".js" or ".mjs" => "text/javascript",
                ".json" or ".map" => "application/json",
                ".svg" => "image/svg+xml",
                ".md" => "text/markdown",
                ".txt" or ".log" or ".xml" => "text/plain",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".ico" => "image/x-icon",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        private static bool IsWorkspaceTextContent(string contentType, string relativePath)
        {
            return contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                || contentType is "application/json" or "text/javascript" or "image/svg+xml"
                || string.IsNullOrEmpty(Path.GetExtension(relativePath));
        }

        #region 会话管理

        /// <summary>
        /// 创建新的聊天会话
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<CreateSessionResponse>> CreateSessionAsync([FromBody] CreateChatSessionInputDto request)
        {
            return await this.GetResponseAsync<AppResponseBase<CreateSessionResponse>, CreateSessionResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var titleSource = string.IsNullOrEmpty(request.Title) ? request.InitialMessage : request.Title;
                var title = string.IsNullOrEmpty(titleSource)
                    ? _localizer["AdminChat.NewConversation"] 
                    : (titleSource.Length > 50 ? titleSource.Substring(0, 50) + "..." : titleSource);

                var workspaceId = request.WorkspaceId?.Trim();
                if (!string.IsNullOrWhiteSpace(workspaceId))
                {
                    if (!IsCurrentAdminSuperAdmin())
                    {
                        throw new NcfExceptionBase("只有超级管理员可以使用 Workspace。");
                    }

                    if (_workspaceStorage == null
                        || await _workspaceStorage.GetAsync(workspaceId).ConfigureAwait(false) == null)
                    {
                        throw new NcfExceptionBase("Workspace 不存在或不可用。");
                    }
                }

                var memory = ServiceProvider.GetRequiredService<AdminChatMemoryService>();
                var memoryMode = request.MemoryMode ?? (request.UseMemory ? AdminChatMemoryMode.Learn : AdminChatMemoryMode.Off);
                await memory.PrepareModeAsync(memoryMode);
                var session = await _sessionService.CreateSessionAsync(
                    title,
                    userId,
                    workspaceId,
                    request.WorkspaceWriteEnabled,
                    request.WorkspaceCliEnabled);
                if (memoryMode != AdminChatMemoryMode.Off)
                {
                    var profile = await memory.GetAsync(userId, session.Id);
                    await memory.SetModeAsync(userId, session.Id, profile.Revision, memoryMode);
                }

                var moduleUidSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (request.ModuleUids != null && request.ModuleUids.Any())
                {
                    foreach (var uid in request.ModuleUids.Where(z => !string.IsNullOrWhiteSpace(z)))
                    {
                        moduleUidSet.Add(uid);
                    }
                }

                moduleUidSet.Add(SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID);

                var modules = new List<(string uid, string name, string version)>();
                foreach (var uid in moduleUidSet)
                {
                    var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == uid);
                    modules.Add((uid, register?.Name ?? uid, register?.Version ?? ""));
                }

                await _sessionModuleService.AddModulesToSessionAsync(session.Id, modules);
                await EnsureAgentsManagerModuleLinkedAsync(session.Id);

                var workflowProvider = ServiceProvider.GetService<IWorkflowFunctionCallingProvider>();
                if (workflowProvider != null && request.WorkflowIds != null)
                {
                    var selectedWorkflowIds = request.WorkflowIds
                        .Where(id => id > 0)
                        .ToHashSet();
                    var availableWorkflows = await workflowProvider
                        .GetAvailableAsync(userId)
                        .ConfigureAwait(false);
                    await _sessionWorkflowService.AddWorkflowsToSessionAsync(
                        session.Id,
                        availableWorkflows
                            .Where(workflow => selectedWorkflowIds.Contains(workflow.Id))
                            .Select(workflow => (
                                workflow.Id,
                                workflow.Name,
                                workflow.Description))
                            .ToList());
                }

                logger.Append($"创建会话: SessionId={session.Id}, UserId={userId}");
                await PublishSyncEventAsync(userId, session.Id, "session-created");

                return new CreateSessionResponse
                {
                    SessionId = session.Id,
                    Title = session.Title,
                    InitialMessage = request.InitialMessage,
                    Mode = request.Mode,
                    WorkspaceId = session.WorkspaceId,
                    WorkspaceWriteEnabled = session.WorkspaceWriteEnabled,
                    WorkspaceCliEnabled = session.WorkspaceCliEnabled
                };
            });
        }

        /// <summary>
        /// 获取用户的会话列表
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetSessionListResponse>> GetSessionListAsync(int pageIndex = 1, int pageSize = 20)
        {
            return await this.GetResponseAsync<AppResponseBase<GetSessionListResponse>, GetSessionListResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var (sessions, totalCount) = await _sessionService.GetUserActiveSessionsAsync(userId, pageIndex, pageSize);
                var sessionDtos = sessions.Select(AdminChatSessionDto.CreateFromEntity).ToList();
                await AttachWorkspaceNamesAsync(sessionDtos);

                return new GetSessionListResponse
                {
                    Sessions = sessionDtos,
                    TotalCount = totalCount
                };
            });
        }

        /// <summary>
        /// 保存当前用户分类内的会话顺序。
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<AdminChatSessionDto>>> ReorderSessionAsync(
            [FromBody] ReorderSessionRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<List<AdminChatSessionDto>>, List<AdminChatSessionDto>>(
                async (response, logger) =>
                {
                    if (request == null) throw new NcfExceptionBase("Session order request required.");
                    var userId = GetCurrentAdminUserInfoId();
                    var sessions = await _sessionService.ReorderSessionAsync(
                        userId, request.SessionId, request.TargetSessionId, request.InsertAfter);
                    logger.Append($"Reordered session: SessionId={request.SessionId}, UserId={userId}");
                    await PublishSyncEventAsync(userId, request.SessionId, "session-reordered");
                    return sessions.Select(AdminChatSessionDto.CreateFromEntity).ToList();
                });
        }

        /// <summary>
        /// 异步生成首轮会话标题。标题生成只读取首条用户消息，不阻塞正常回复流。
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<GenerateSessionTitleResponse>> GenerateSessionTitleAsync(
            [FromBody] GenerateSessionTitleRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<GenerateSessionTitleResponse>, GenerateSessionTitleResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(request?.SessionId ?? 0, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var (messages, _) = await _messageService.GetSessionMessagesAsync(session.Id);
                var firstUserMessage = messages
                    .Where(message => message.RoleType == ChatMessageRoleType.User)
                    .OrderBy(message => message.Sequence)
                    .FirstOrDefault();
                if (firstUserMessage == null)
                {
                    return new GenerateSessionTitleResponse { Title = session.Title };
                }

                var title = await _chatAiService.GenerateSessionTitleAsync(
                    firstUserMessage.Content,
                    request?.AiModelId ?? 0);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    await _sessionService.UpdateSessionTitleAsync(session.Id, userId, title);
                }

                logger.Append($"生成会话标题: SessionId={session.Id}, UserId={userId}");
                return new GenerateSessionTitleResponse
                {
                    Title = string.IsNullOrWhiteSpace(title) ? session.Title : title
                };
            });
        }

        /// <summary>
        /// 获取会话详情（包含消息和模块）
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetSessionDetailResponse>> GetSessionDetailAsync(int sessionId)
        {
            return await this.GetResponseAsync<AppResponseBase<GetSessionDetailResponse>, GetSessionDetailResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(sessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                await EnsureAgentsManagerModuleLinkedAsync(sessionId);

                var (messages, _) = await _messageService.GetSessionMessagesAsync(sessionId);
                var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);
                var workflows = await _sessionWorkflowService.GetSessionWorkflowsAsync(sessionId);

                var sessionDto = AdminChatSessionDto.CreateFromEntity(session);
                await AttachWorkspaceNamesAsync(new[] { sessionDto });
                sessionDto.Messages = messages.Select(AdminChatMessageDto.CreateFromEntity).ToList();
                sessionDto.Modules = modules.Select(z => MapModuleDtoWithRegisterInfo(AdminChatSessionModuleDto.CreateFromEntity(z))).ToList();
                sessionDto.Workflows = await MapWorkflowDtosAsync(workflows, userId).ConfigureAwait(false);

                return new GetSessionDetailResponse
                {
                    Session = sessionDto
                };
            });
        }

        /// <summary>
        /// 删除会话
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> DeleteSessionAsync(int sessionId)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var success = await _sessionService.DeleteSessionAsync(sessionId, userId);
                if (!success)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrNoDeletePermission"]);
                }

                logger.Append($"删除会话: SessionId={sessionId}, UserId={userId}");
                await PublishSyncEventAsync(userId, sessionId, "session-deleted");
                return _localizer["AdminChat.DeleteSessionSuccess"];
            });
        }

        /// <summary>
        /// 【超级管理员】按用户统计 AdminChat 使用量：仅返回各账号的会话/消息数量与最后活跃时间，不返回任何会话或消息内容。
        /// <para>普通账号仅能访问自己的会话；超级管理员（administrator 角色）通过本接口做用量监控。</para>
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetUserSessionStatsResponse>> GetUserSessionStatsAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<GetUserSessionStatsResponse>, GetUserSessionStatsResponse>(async (response, logger) =>
            {
                var currentUserId = GetCurrentAdminUserInfoId();
                if (currentUserId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                if (!IsCurrentAdminSuperAdmin())
                {
                    logger.Append($"拒绝非超级管理员访问用户统计: UserId={currentUserId}");
                    throw new NcfExceptionBase(_localizer["AdminChat.SuperAdminOnly"]);
                }

                var sessionStats = await _sessionService.GetUserSessionStatsAsync();
                var messageCounts = await _messageService.GetMessageCountByUserAsync();
                var messageCountMap = messageCounts.ToDictionary(x => x.UserId, x => x.Count);

                var userIds = sessionStats.Select(s => s.UserId).ToList();
                var users = _adminUserInfoService.GetAdminUserInfo(userIds);
                var userMap = users.ToDictionary(u => u.Id);

                var items = sessionStats
                    .OrderByDescending(s => s.LastActiveTime)
                    .Select(s =>
                    {
                        userMap.TryGetValue(s.UserId, out var user);
                        return new AdminChatUserStatDto
                        {
                            UserId = s.UserId,
                            UserName = user?.UserName,
                            RealName = user?.RealName,
                            TotalSessionCount = s.TotalSessionCount,
                            ActiveSessionCount = s.ActiveSessionCount,
                            ArchivedSessionCount = s.ArchivedSessionCount,
                            DeletedSessionCount = s.DeletedSessionCount,
                            MessageCount = messageCountMap.TryGetValue(s.UserId, out var count) ? count : 0,
                            LastActiveTime = s.LastActiveTime
                        };
                    })
                    .ToList();

                logger.Append($"超级管理员用户统计: CurrentUserId={currentUserId}, UserCount={items.Count}");
                return new GetUserSessionStatsResponse
                {
                    Stats = items
                };
            });
        }

        #endregion

        private async Task AttachWorkspaceNamesAsync(IEnumerable<AdminChatSessionDto> sessions)
        {
            if (_workspaceStorage == null || !IsCurrentAdminSuperAdmin())
            {
                return;
            }

            var items = sessions?.Where(session => !string.IsNullOrWhiteSpace(session.WorkspaceId)).ToList();
            if (items == null || items.Count == 0)
            {
                return;
            }

            var workspaces = await _workspaceStorage.ListWorkspacesAsync().ConfigureAwait(false);
            var names = workspaces
                .Where(workspace => workspace != null && !string.IsNullOrWhiteSpace(workspace.Id))
                .ToDictionary(workspace => workspace.Id, workspace => workspace.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var session in items)
            {
                if (names.TryGetValue(session.WorkspaceId, out var name))
                {
                    session.WorkspaceName = name;
                }
            }
        }

        #region 消息管理

        /// <summary>
        /// 发送消息
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<SendMessageResponse>> SendMessageAsync([FromBody] ChatMessageInputDto request)
        {
            return await this.GetResponseAsync<AppResponseBase<SendMessageResponse>, SendMessageResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(request.SessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                if (string.IsNullOrWhiteSpace(request.Content) && request.Attachments?.Count is not > 0)
                    throw new NcfExceptionBase(_localizer["AdminChat.StreamInputRequired"]);
                if (request.Attachments?.Count > 0) await GetWorkspaceSessionAsync(request.SessionId);
                var attachments = await ServiceProvider.GetRequiredService<AdminChatAttachmentService>()
                    .ValidateAsync(session, request.Attachments);
                var content = string.IsNullOrWhiteSpace(request.Content)
                    ? AdminChatAttachmentService.AttachmentOnlyMessage : request.Content.Trim();
                var userMessage = await _messageService.AddMessageAsync(
                    request.SessionId,
                    ChatMessageRoleType.User,
                    content, attachments: attachments);

                await _sessionService.UpdateLastMessageTimeAsync(request.SessionId);

                var mode = request.Mode;
                string aiResponse;
                string modelIdentifier;
                AdminChatHarnessResult harness = null;

                if (mode == AdminChatMode.Harness)
                {
                    (aiResponse, modelIdentifier, harness) = await _chatAiService.GenerateNativeHarnessResponseAsync(
                        request.SessionId,
                        userId,
                        content,
                        request.AiModelId, attachments: attachments);
                }
                else
                {
                    (aiResponse, modelIdentifier) = await _chatAiService.GenerateResponseAsync(request.SessionId, userId, content, request.AiModelId, attachments: attachments);
                }

                var assistantMessage = await _messageService.AddMessageAsync(
                    request.SessionId,
                    ChatMessageRoleType.Assistant,
                    aiResponse,
                    modelIdentifier,
                    harness?.TrajectoryId,
                    harness?.TrajectorySequence);

                logger.Append($"发送消息: SessionId={request.SessionId}, MessageId={userMessage.Id}, Mode={mode}");
                await PublishSyncEventAsync(userId, request.SessionId, "messages-changed");

                return new SendMessageResponse
                {
                    MemoryUpdateStatus = _chatAiService.MemoryUpdateStatus,
                    MemoryUpdateResult = _chatAiService.MemoryUpdateResult,
                    MemoryUsage = _chatAiService.MemoryUsage,
                    UserMessage = AdminChatMessageDto.CreateFromEntity(userMessage),
                    AssistantMessage = AdminChatMessageDto.CreateFromEntity(assistantMessage),
                    Mode = mode,
                    HarnessSteps = harness?.Steps,
                    TrajectoryId = harness?.TrajectoryId ?? 0,
                    TrajectorySequence = harness?.TrajectorySequence ?? 0,
                    TrajectoryStatus = harness?.Status ?? default,
                    TrajectoryEvents = harness?.TrajectoryEvents,
                    PendingApprovals = harness?.PendingApprovals
                };
            });
        }

        /// <summary>
        /// 获取会话的消息列表
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetMessagesResponse>> GetMessagesAsync(int sessionId, int pageIndex = 0, int pageSize = 50)
        {
            return await this.GetResponseAsync<AppResponseBase<GetMessagesResponse>, GetMessagesResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(sessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var (messages, totalCount) = await _messageService.GetSessionMessagesAsync(sessionId, pageIndex, pageSize);

                return new GetMessagesResponse
                {
                    Messages = messages.Select(AdminChatMessageDto.CreateFromEntity).ToList(),
                    TotalCount = totalCount
                };
            });
        }

        /// <summary>
        /// 获取 Harness Trajectory 及其事件。Trajectory 内容只通过显式接口返回，不混入普通消息历史。
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetTrajectoryResponse>> GetTrajectoryAsync(
            int trajectoryId,
            string query = null,
            int beforeSequence = 0,
            int pageSize = 200)
        {
            return await this.GetResponseAsync<AppResponseBase<GetTrajectoryResponse>, GetTrajectoryResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var trajectory = await _trajectoryService.GetAsync(trajectoryId, userId);
                if (trajectory == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.TrajectoryNotFound"]);
                }

                var events = await _trajectoryService.GetEventsAsync(
                    trajectoryId,
                    userId,
                    beforeSequence > 0 ? beforeSequence : null,
                    query,
                    pageSize);

                return new GetTrajectoryResponse
                {
                    Trajectory = AdminChatTrajectoryDto.CreateFromEntity(trajectory),
                    Events = AdminChatTrajectoryEventDto.CollapseAssistantTextChunks(
                        events.Select(AdminChatTrajectoryEventDto.CreateFromEntity)).ToList()
                };
            });
        }

        /// <summary>
        /// 在当前管理员可见的 Harness Trajectory 中检索事件内容。
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<SearchTrajectoriesResponse>> SearchTrajectoriesAsync(
            string query,
            int pageSize = 50)
        {
            return await this.GetResponseAsync<AppResponseBase<SearchTrajectoriesResponse>, SearchTrajectoriesResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var trajectories = await _trajectoryService.SearchAsync(userId, query, pageSize);
                return new SearchTrajectoriesResponse
                {
                    Trajectories = trajectories.Select(AdminChatTrajectoryDto.CreateFromEntity).ToList()
                };
            });
        }

        /// <summary>
        /// 获取当前会话的全部 Harness Trajectory 索引。
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<SearchTrajectoriesResponse>> GetSessionTrajectoriesAsync(int sessionId)
        {
            return await this.GetResponseAsync<AppResponseBase<SearchTrajectoriesResponse>, SearchTrajectoriesResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                if (await _sessionService.GetSessionByIdAsync(sessionId, userId) == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var trajectories = await _trajectoryService.GetSessionTrajectoriesAsync(sessionId, userId);
                return new SearchTrajectoriesResponse
                {
                    Trajectories = trajectories.Select(AdminChatTrajectoryDto.CreateFromEntity).ToList()
                };
            });
        }

        /// <summary>
        /// Resume a stored native MAF Harness Trajectory.
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<SendTrajectoryResponse>> ResumeTrajectoryAsync(
            [FromBody] ResumeTrajectoryRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<SendTrajectoryResponse>, SendTrajectoryResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                var trajectory = await _trajectoryService.GetAsync(request?.TrajectoryId ?? 0, userId);
                if (trajectory == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.TrajectoryNotFound"]);
                }

                var userMessage = await _messageService.AddMessageAsync(
                    trajectory.SessionId,
                    ChatMessageRoleType.User,
                    request.Instruction ?? string.Empty);
                var (aiResponse, modelIdentifier, harness) = await _chatAiService.ResumeNativeHarnessResponseAsync(
                    trajectory.Id,
                    userId,
                    request.Instruction,
                    request.AiModelId);
                var assistantMessage = await _messageService.AddMessageAsync(
                    trajectory.SessionId,
                    ChatMessageRoleType.Assistant,
                    aiResponse,
                    modelIdentifier,
                    harness.TrajectoryId,
                    harness.TrajectorySequence);
                await _sessionService.UpdateLastMessageTimeAsync(trajectory.SessionId);
                await PublishSyncEventAsync(userId, trajectory.SessionId, "messages-changed");

                return new SendTrajectoryResponse
                {
                    MemoryUpdateStatus = _chatAiService.MemoryUpdateStatus,
                    MemoryUpdateResult = _chatAiService.MemoryUpdateResult,
                    MemoryUsage = _chatAiService.MemoryUsage,
                    SessionId = trajectory.SessionId,
                    UserMessage = AdminChatMessageDto.CreateFromEntity(userMessage),
                    AssistantMessage = AdminChatMessageDto.CreateFromEntity(assistantMessage),
                    Harness = harness
                };
            });
        }

        /// <summary>
        /// Fork a new native MAF Harness Trajectory from an existing event sequence.
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<SendTrajectoryResponse>> ForkTrajectoryAsync(
            [FromBody] ForkTrajectoryRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<SendTrajectoryResponse>, SendTrajectoryResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                var parent = await _trajectoryService.GetAsync(request?.TrajectoryId ?? 0, userId);
                if (parent == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.TrajectoryNotFound"]);
                }

                var branchTitle = string.IsNullOrWhiteSpace(request.Instruction)
                    ? $"分叉：{parent.Title}"
                    : (request.Instruction.Length > 50
                        ? request.Instruction.Substring(0, 50) + "..."
                        : request.Instruction);
                var branchSession = await _sessionService.CreateSessionAsync(branchTitle, userId);
                var parentModules = await _sessionModuleService.GetSessionModulesAsync(parent.SessionId);
                await _sessionModuleService.AddModulesToSessionAsync(
                    branchSession.Id,
                    parentModules.Select(module => (
                        module.XncfModuleUid,
                        module.ModuleName,
                        module.ModuleVersion)).ToList());
                var parentWorkflows = await _sessionWorkflowService.GetSessionWorkflowsAsync(parent.SessionId);
                await _sessionWorkflowService.AddWorkflowsToSessionAsync(
                    branchSession.Id,
                    parentWorkflows.Select(workflow => (
                        workflow.WorkflowId,
                        workflow.WorkflowName,
                        workflow.WorkflowDescription)));

                var userMessage = await _messageService.AddMessageAsync(
                    branchSession.Id,
                    ChatMessageRoleType.User,
                    request.Instruction ?? string.Empty);
                var (aiResponse, modelIdentifier, harness) = await _chatAiService.ForkNativeHarnessResponseAsync(
                    parent.Id,
                    userId,
                    branchSession.Id,
                    request.ForkFromSequence,
                    request.Instruction,
                    request.AiModelId);
                var assistantMessage = await _messageService.AddMessageAsync(
                    branchSession.Id,
                    ChatMessageRoleType.Assistant,
                    aiResponse,
                    modelIdentifier,
                    harness.TrajectoryId,
                    harness.TrajectorySequence);
                await _sessionService.UpdateLastMessageTimeAsync(branchSession.Id);
                await PublishSyncEventAsync(userId, branchSession.Id, "messages-changed");

                return new SendTrajectoryResponse
                {
                    MemoryUpdateStatus = _chatAiService.MemoryUpdateStatus,
                    MemoryUpdateResult = _chatAiService.MemoryUpdateResult,
                    MemoryUsage = _chatAiService.MemoryUsage,
                    SessionId = branchSession.Id,
                    UserMessage = AdminChatMessageDto.CreateFromEntity(userMessage),
                    AssistantMessage = AdminChatMessageDto.CreateFromEntity(assistantMessage),
                    Harness = harness
                };
            });
        }

        /// <summary>
        /// Approve or reject a tool call waiting in a native MAF Harness Trajectory.
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<SendTrajectoryResponse>> RespondTrajectoryApprovalAsync(
            [FromBody] TrajectoryApprovalRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<SendTrajectoryResponse>, SendTrajectoryResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                var trajectory = await _trajectoryService.GetAsync(request?.TrajectoryId ?? 0, userId);
                if (trajectory == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.TrajectoryNotFound"]);
                }

                var (aiResponse, modelIdentifier, harness) = await _chatAiService.RespondNativeHarnessApprovalAsync(
                    trajectory.Id,
                    userId,
                    request.RequestId,
                    request.ToolCallId,
                    request.ToolName,
                    request.ArgumentsJson,
                    request.Approved,
                    request.Reason,
                    request.AiModelId);
                var assistantMessage = await _messageService.AddMessageAsync(
                    trajectory.SessionId,
                    ChatMessageRoleType.Assistant,
                    aiResponse,
                    modelIdentifier,
                    harness.TrajectoryId,
                    harness.TrajectorySequence);
                await _sessionService.UpdateLastMessageTimeAsync(trajectory.SessionId);
                await PublishSyncEventAsync(userId, trajectory.SessionId, "messages-changed");

                return new SendTrajectoryResponse
                {
                    MemoryUpdateStatus = "unchanged",
                    MemoryUpdateResult = _chatAiService.MemoryUpdateResult,
                    MemoryUsage = _chatAiService.MemoryUsage,
                    SessionId = trajectory.SessionId,
                    AssistantMessage = AdminChatMessageDto.CreateFromEntity(assistantMessage),
                    Harness = harness
                };
            });
        }

        /// <summary>
        /// 设置消息反馈
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Put)]
        public async Task<StringAppResponse> SetMessageFeedbackAsync(int messageId, MessageFeedbackType feedback)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                //归属校验：消息所属会话必须属于当前用户，防止跨账号越权设置反馈
                var success = await _messageService.SetMessageFeedbackAsync(messageId, userId, feedback);
                if (!success)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.MessageNotFound"]);
                }

                logger.Append($"设置反馈: MessageId={messageId}, UserId={userId}, Feedback={feedback}");
                return _localizer["AdminChat.SetFeedbackSuccess"];
            });
        }

        /// <summary>
        /// 批量删除消息
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> DeleteMessagesAsync(int sessionId, string messageIds)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(sessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var parsedMessageIds = (messageIds ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id, out var value) ? value : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (!parsedMessageIds.Any())
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SelectAtLeastOneMessage"]);
                }

                var deletedCount = await _messageService.DeleteMessagesAsync(sessionId, parsedMessageIds);
                logger.Append($"批量删除消息: SessionId={sessionId}, DeletedCount={deletedCount}");
                await PublishSyncEventAsync(userId, sessionId, "messages-changed");
                return _localizer["AdminChat.DeleteMessagesSuccess", deletedCount];
            });
        }

        #endregion

        #region 模块管理

        /// <summary>
        /// 添加模块到会话
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> AddModulesToSessionAsync([FromBody] AddModulesRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(request.SessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var modules = (request.Modules ?? new List<ModuleInfo>())
                    .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Uid))
                    .GroupBy(m => m.Uid, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .Select(m => (m.Uid, m.Name, m.Version ?? ""))
                    .ToList();
                if (!modules.Any())
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SelectAtLeastOneModule"]);
                }

                await _sessionModuleService.AddModulesToSessionAsync(request.SessionId, modules);

                logger.Append($"添加模块: SessionId={request.SessionId}, Count={modules.Count}");
                await PublishSyncEventAsync(userId, request.SessionId, "modules-changed");
                return _localizer["AdminChat.AddModulesSuccess"];
            });
        }

        /// <summary>
        /// 将会话的模块关联更新为指定集合；系统模块管理器始终保留。
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> SetSessionModulesAsync([FromBody] AddModulesRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(request.SessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var requestedUids = (request.Modules ?? new List<ModuleInfo>())
                    .Where(module => module != null && !string.IsNullOrWhiteSpace(module.Uid))
                    .Select(module => module.Uid)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                requestedUids.Add(SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID);

                var registers = XncfRegisterManager.RegisterList
                    .Where(register => register != null && requestedUids.Contains(register.Uid))
                    .GroupBy(register => register.Uid, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToArray();
                var availabilityService = ServiceProvider.GetRequiredService<INeuBellModuleAvailabilityService>();
                var openModuleUids = await availabilityService.GetOpenModuleUidsAsync(registers.Select(register => register.Uid));
                openModuleUids = openModuleUids
                    .Append(SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var desiredModules = registers
                    .Where(register => openModuleUids.Contains(register.Uid))
                    .Select(register => (register.Uid, register.Name, register.Version ?? string.Empty))
                    .ToList();
                if (desiredModules.All(module => !string.Equals(
                        module.Uid,
                        SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    desiredModules.Add((
                        SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID,
                        SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID,
                        string.Empty));
                }

                var desiredUidSet = desiredModules
                    .Select(module => module.Uid)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var existingModules = await _sessionModuleService.GetSessionModulesAsync(request.SessionId);
                // 先幂等添加目标集合，再移除多余关联；若中途失败，最多暂时保留旧关联，
                // 不会先删掉用户原有的可用模块。
                await _sessionModuleService.AddModulesToSessionAsync(request.SessionId, desiredModules);
                foreach (var existingModule in existingModules.Where(module => !desiredUidSet.Contains(module.XncfModuleUid)))
                {
                    await _sessionModuleService.RemoveModuleFromSessionAsync(request.SessionId, existingModule.XncfModuleUid);
                }

                logger.Append($"更新模块关联: SessionId={request.SessionId}, Count={desiredModules.Count}");
                await PublishSyncEventAsync(userId, request.SessionId, "modules-changed");
                return _localizer["AdminChat.AddModulesSuccess"];
            });
        }

        /// <summary>
        /// 获取会话的模块列表
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetSessionModulesResponse>> GetSessionModulesAsync(int sessionId)
        {
            return await this.GetResponseAsync<AppResponseBase<GetSessionModulesResponse>, GetSessionModulesResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(sessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);

                return new GetSessionModulesResponse
                {
                    Modules = modules.Select(z => MapModuleDtoWithRegisterInfo(AdminChatSessionModuleDto.CreateFromEntity(z))).ToList()
                };
            });
        }

        /// <summary>
        /// 将会话的 Workflow 关联更新为指定集合。
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> SetSessionWorkflowsAsync([FromBody] SetWorkflowsRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(request.SessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var provider = ServiceProvider.GetService<IWorkflowFunctionCallingProvider>();
                var available = provider == null
                    ? Array.Empty<WorkflowFunctionCallingDescriptor>()
                    : await provider.GetAvailableAsync(userId).ConfigureAwait(false);
                var requestedIds = (request.WorkflowIds ?? new List<int>())
                    .Where(id => id > 0)
                    .ToHashSet();
                var desired = available
                    .Where(workflow => requestedIds.Contains(workflow.Id))
                    .Select(workflow => (
                        workflow.Id,
                        workflow.Name,
                        workflow.Description))
                    .ToList();

                await _sessionWorkflowService.SetSessionWorkflowsAsync(request.SessionId, desired);
                logger.Append($"更新 Workflow 关联: SessionId={request.SessionId}, Count={desired.Count}");
                await PublishSyncEventAsync(userId, request.SessionId, "workflows-changed");
                return _localizer["AdminChat.AddModulesSuccess"];
            });
        }

        /// <summary>
        /// 获取当前已安装并开放、可关联到 Admin Chat 会话的 XNCF 模块。
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetAvailableModulesResponse>> GetAvailableModulesAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<GetAvailableModulesResponse>, GetAvailableModulesResponse>(async (response, logger) =>
            {
                var registers = XncfRegisterManager.RegisterList
                    .Where(register => register != null && !string.IsNullOrWhiteSpace(register.Uid))
                    .GroupBy(register => register.Uid, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToArray();
                var availabilityService = ServiceProvider.GetRequiredService<INeuBellModuleAvailabilityService>();
                var openModuleUids = await availabilityService.GetOpenModuleUidsAsync(registers.Select(register => register.Uid));

                var modules = registers
                    .Where(register => openModuleUids.Contains(register.Uid))
                    .Select(register => new AvailableModuleDto
                    {
                        Uid = register.Uid,
                        Name = register.Name,
                        DisplayName = string.IsNullOrWhiteSpace(register.MenuName) ? register.Name : register.MenuName,
                        Version = register.Version ?? string.Empty,
                        Description = register.Description ?? string.Empty,
                        Icon = register.Icon ?? string.Empty,
                        IsRequired = string.Equals(
                            register.Uid,
                            SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID,
                            StringComparison.OrdinalIgnoreCase)
                    })
                    .OrderBy(module => module.DisplayName)
                    .ToList();

                return new GetAvailableModulesResponse
                {
                    Modules = modules
                };
            });
        }

        /// <summary>
        /// 获取当前管理员可用于 AdminChat Function Calling 的已启用 Workflow。
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetAvailableWorkflowsResponse>> GetAvailableWorkflowsAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<GetAvailableWorkflowsResponse>, GetAvailableWorkflowsResponse>(
                async (response, logger) =>
                {
                    var userId = GetCurrentAdminUserInfoId();
                    if (userId <= 0)
                    {
                        throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                    }

                    var provider = ServiceProvider.GetService<IWorkflowFunctionCallingProvider>();
                    var workflows = provider == null
                        ? Array.Empty<WorkflowFunctionCallingDescriptor>()
                        : await provider.GetAvailableAsync(userId).ConfigureAwait(false);
                    return new GetAvailableWorkflowsResponse
                    {
                        Workflows = workflows.Select(MapAvailableWorkflowDto).ToList()
                    };
                });
        }

        /// <summary>
        /// 获取 Chat 可选 AI 模型列表。
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetAiModelOptionsResponse>> GetAiModelOptionsAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<GetAiModelOptionsResponse>, GetAiModelOptionsResponse>(async (response, logger) =>
            {
                var optionList = new List<AiModelOptionDto>
                {
                    new AiModelOptionDto
                    {
                        Id = 0,
                        Name = _localizer["AdminChat.FallbackModelName"].Value,
                        Description = _localizer["AdminChat.FallbackModelDescription"].Value,
                        IsDefault = true
                    }
                };

                var aiKernelRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z =>
                    string.Equals(z.Name, "Senparc.Xncf.AIKernel", StringComparison.OrdinalIgnoreCase));

                var aiKernelAvailable = false;
                if (aiKernelRegister != null)
                {
                    var registerManager = new XncfRegisterManager(ServiceProvider);
                    aiKernelAvailable = await registerManager.CheckXncfAvailable(aiKernelRegister);
                }

                if (aiKernelAvailable)
                {
                    var aiModelService = ServiceProvider.GetService(typeof(AIModelService)) as AIModelService;
                    if (aiModelService != null)
                    {
                        var aiModels = await aiModelService.GetFullListAsync(z =>
                            z.Show && z.ConfigModelType == Senparc.Xncf.AIKernel.Domain.Models.ConfigModelType.Chat);

                        optionList.AddRange(aiModels
                            .OrderByDescending(z => z.Show)
                            .ThenBy(z => z.Alias)
                            .Select(z => new AiModelOptionDto
                            {
                                Id = z.Id,
                                Name = !string.IsNullOrWhiteSpace(z.Alias)
                                    ? $"{z.Alias} ({z.ModelId})"
                                    : $"{z.DeploymentName} ({z.ModelId})",
                                Description = string.IsNullOrWhiteSpace(z.Note)
                                    ? $"{z.AiPlatform} | {z.Endpoint}"
                                    : z.Note,
                                IsDefault = false
                            }));
                    }
                }

                return new GetAiModelOptionsResponse
                {
                    AiKernelAvailable = aiKernelAvailable,
                    Models = optionList
                };
            });
        }

        private static AdminChatSessionModuleDto MapModuleDtoWithRegisterInfo(AdminChatSessionModuleDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == dto.XncfModuleUid);
            if (register != null)
            {
                dto.ModuleName = string.IsNullOrWhiteSpace(dto.ModuleName) ? register.Name : dto.ModuleName;
                dto.ModuleVersion = string.IsNullOrWhiteSpace(dto.ModuleVersion) ? register.Version : dto.ModuleVersion;
                dto.MenuName = register.MenuName;
                dto.ModuleDescription = register.Description;
                dto.DisplayName = !string.IsNullOrWhiteSpace(register.MenuName) ? register.MenuName : register.Name;
            }

            if (string.IsNullOrWhiteSpace(dto.DisplayName))
            {
                dto.DisplayName = dto.ModuleName;
            }

            return dto;
        }

        private async Task EnsureAgentsManagerModuleLinkedAsync(int sessionId)
        {
            var register = XncfRegisterManager.RegisterList.FirstOrDefault(module =>
                string.Equals(module.Uid, AgentsManagerModuleUid, StringComparison.OrdinalIgnoreCase));
            if (register == null)
            {
                return;
            }

            var availabilityService = ServiceProvider.GetRequiredService<INeuBellModuleAvailabilityService>();
            var openModuleUids = await availabilityService.GetOpenModuleUidsAsync(new[] { register.Uid });
            if (openModuleUids.Contains(register.Uid, StringComparer.OrdinalIgnoreCase))
            {
                await _sessionModuleService.AddModuleToSessionAsync(
                    sessionId,
                    register.Uid,
                    register.Name,
                    register.Version ?? string.Empty);
            }
        }

        private async Task<List<AdminChatSessionWorkflowDto>> MapWorkflowDtosAsync(
            IReadOnlyList<AdminChatSessionWorkflow> workflows,
            int userId)
        {
            var provider = ServiceProvider.GetService<IWorkflowFunctionCallingProvider>();
            var available = provider == null
                ? Array.Empty<WorkflowFunctionCallingDescriptor>()
                : await provider.GetAvailableAsync(userId).ConfigureAwait(false);
            var availableById = available.ToDictionary(workflow => workflow.Id);

            return workflows.Select(workflow =>
            {
                var dto = AdminChatSessionWorkflowDto.CreateFromEntity(workflow);
                if (availableById.TryGetValue(workflow.WorkflowId, out var descriptor))
                {
                    dto.WorkflowName = descriptor.Name;
                    dto.WorkflowDescription = descriptor.Description;
                    dto.Parameters = descriptor.Parameters
                        .Select(parameter => new WorkflowFunctionCallingParameterDto
                        {
                            Name = parameter.Name,
                            Description = parameter.Description
                        })
                        .ToList();
                }
                return dto;
            }).ToList();
        }

        private static AvailableWorkflowDto MapAvailableWorkflowDto(
            WorkflowFunctionCallingDescriptor workflow) =>
            new()
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Description = workflow.Description ?? string.Empty,
                Parameters = workflow.Parameters
                    .Select(parameter => new WorkflowParameterDto
                    {
                        Name = parameter.Name,
                        Description = parameter.Description ?? string.Empty
                    })
                    .ToList()
            };

        #endregion

        #region 私有辅助方法

        #endregion

        private ValueTask PublishSyncEventAsync(int userId, int sessionId, string action)
        {
            return _eventBus?.PublishAsync(new AdminChatSyncEvent(userId, sessionId, action))
                   ?? ValueTask.CompletedTask;
        }
    }

    #region 请求和响应模型

    /// <summary>
    /// 创建会话响应
    /// </summary>
    public class CreateSessionResponse
    {
        public int SessionId { get; set; }
        public string Title { get; set; }
        public string InitialMessage { get; set; }
        public AdminChatMode Mode { get; set; }
        public string WorkspaceId { get; set; }
        public bool WorkspaceWriteEnabled { get; set; }
        public bool WorkspaceCliEnabled { get; set; }
    }

    /// <summary>
    /// 获取会话列表响应
    /// </summary>
    public class ReorderSessionRequest
    {
        public int SessionId { get; set; }
        public int TargetSessionId { get; set; }
        public bool InsertAfter { get; set; }
    }

    public class GetSessionListResponse
    {
        public List<AdminChatSessionDto> Sessions { get; set; }
        public int TotalCount { get; set; }
    }

    public class GetWorkspaceOptionsResponse
    {
        public IReadOnlyList<WorkspaceOptionDto> Workspaces { get; set; } = Array.Empty<WorkspaceOptionDto>();
    }

    public class CreateWorkspaceRequest
    {
        public string Name { get; set; }
    }

    public class GenerateSessionTitleRequest
    {
        public int SessionId { get; set; }
        public int AiModelId { get; set; }
    }

    public class GenerateSessionTitleResponse
    {
        public string Title { get; set; }
    }

    public class WorkspaceOptionDto
    {
        public string Id { get; set; }
        public int OwnerId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime LastAccessedUtc { get; set; }

        public static WorkspaceOptionDto CreateFromDescriptor(WorkspaceDescriptor descriptor)
        {
            return new WorkspaceOptionDto
            {
                Id = descriptor.Id,
                OwnerId = descriptor.OwnerId,
                Name = descriptor.Name,
                CreatedUtc = descriptor.CreatedUtc,
                LastAccessedUtc = descriptor.LastAccessedUtc
            };
        }
    }

    public class WorkspaceEntriesResponse
    {
        public string CurrentPath { get; set; } = string.Empty;
        public IReadOnlyList<WorkspaceEntry> Entries { get; set; } = Array.Empty<WorkspaceEntry>();
    }

    public class WorkspaceFilePreviewDto
    {
        public string Path { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public long Size { get; set; }
        public bool IsText { get; set; }
        public string Content { get; set; }
        public string ContentBase64 { get; set; } = string.Empty;
    }

    /// <summary>
    /// 【超级管理员】单账号 AdminChat 用量统计（仅数量，不含内容）
    /// </summary>
    public class AdminChatUserStatDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string RealName { get; set; }
        public int TotalSessionCount { get; set; }
        public int ActiveSessionCount { get; set; }
        public int ArchivedSessionCount { get; set; }
        public int DeletedSessionCount { get; set; }
        public int MessageCount { get; set; }
        public DateTime LastActiveTime { get; set; }
    }

    /// <summary>
    /// 【超级管理员】用户用量统计响应
    /// </summary>
    public class GetUserSessionStatsResponse
    {
        public List<AdminChatUserStatDto> Stats { get; set; } = new List<AdminChatUserStatDto>();
    }

    /// <summary>
    /// 获取会话详情响应
    /// </summary>
    public class GetSessionDetailResponse
    {
        public AdminChatSessionDto Session { get; set; }
    }

    /// <summary>
    /// 发送消息响应
    /// </summary>
    public class SendMessageResponse
    {
        public string MemoryUpdateStatus { get; set; }
        public AdminChatMemoryUpdateResult MemoryUpdateResult { get; set; }
        public AdminChatMemoryUsage MemoryUsage { get; set; }
        public AdminChatMessageDto UserMessage { get; set; }
        public AdminChatMessageDto AssistantMessage { get; set; }

        /// <summary>
        /// 本次运行使用的模式（Simple 或 Harness）。
        /// </summary>
        public AdminChatMode Mode { get; set; } = AdminChatMode.Simple;

        /// <summary>
        /// Harness 模式的执行步骤记录（Simple 模式为 null）。
        /// </summary>
        public IReadOnlyList<AdminChatHarnessStep> HarnessSteps { get; set; }

        /// <summary>
        /// Native MAF Harness Trajectory identifier.
        /// </summary>
        public int TrajectoryId { get; set; }

        /// <summary>
        /// Native MAF Harness Trajectory status.
        /// </summary>
        public AdminChatTrajectoryStatus TrajectoryStatus { get; set; }

        public int TrajectorySequence { get; set; }

        /// <summary>
        /// Events produced during the current Harness request.
        /// </summary>
        public IReadOnlyList<AdminChatTrajectoryEventDto> TrajectoryEvents { get; set; }

        /// <summary>
        /// Tool approvals waiting for the user.
        /// </summary>
        public IReadOnlyList<AdminChatApprovalRequestDto> PendingApprovals { get; set; }
    }

    /// <summary>
    /// 获取消息列表响应
    /// </summary>
    public class GetMessagesResponse
    {
        public List<AdminChatMessageDto> Messages { get; set; }
        public int TotalCount { get; set; }
    }

    public class GetTrajectoryResponse
    {
        public AdminChatTrajectoryDto Trajectory { get; set; }
        public List<AdminChatTrajectoryEventDto> Events { get; set; }
    }

    public class SearchTrajectoriesResponse
    {
        public List<AdminChatTrajectoryDto> Trajectories { get; set; }
    }

    public class ResumeTrajectoryRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int TrajectoryId { get; set; }
        public string Instruction { get; set; }
        public int AiModelId { get; set; }
    }

    public class ForkTrajectoryRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int TrajectoryId { get; set; }
        public int ForkFromSequence { get; set; }
        public string Instruction { get; set; }
        public int AiModelId { get; set; }
    }

    public class TrajectoryApprovalRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int TrajectoryId { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string RequestId { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string ToolCallId { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string ToolName { get; set; }
        public string ArgumentsJson { get; set; }
        public bool Approved { get; set; }
        public string Reason { get; set; }
        public int AiModelId { get; set; }
    }

    public class SendTrajectoryResponse
    {
        public string MemoryUpdateStatus { get; set; }
        public AdminChatMemoryUpdateResult MemoryUpdateResult { get; set; }
        public AdminChatMemoryUsage MemoryUsage { get; set; }
        public int SessionId { get; set; }
        public AdminChatMessageDto UserMessage { get; set; }
        public AdminChatMessageDto AssistantMessage { get; set; }
        public AdminChatHarnessResult Harness { get; set; }
    }

    /// <summary>
    /// 添加模块到会话请求
    /// </summary>
    public class AddModulesRequest
    {
        public int SessionId { get; set; }
        public List<ModuleInfo> Modules { get; set; }
    }

    public class SetWorkflowsRequest
    {
        public int SessionId { get; set; }
        public List<int> WorkflowIds { get; set; } = new();
    }

    /// <summary>
    /// 模块信息
    /// </summary>
    public class ModuleInfo
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
    }

    /// <summary>
    /// 获取会话模块列表响应
    /// </summary>
    public class GetSessionModulesResponse
    {
        public List<AdminChatSessionModuleDto> Modules { get; set; }
    }

    /// <summary>
    /// 获取可关联模块列表响应。
    /// </summary>
    public class GetAvailableModulesResponse
    {
        public List<AvailableModuleDto> Modules { get; set; }
    }

    public class GetAvailableWorkflowsResponse
    {
        public List<AvailableWorkflowDto> Workflows { get; set; } = new();
    }

    public class AvailableWorkflowDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<WorkflowParameterDto> Parameters { get; set; } = new();
    }

    public class WorkflowParameterDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 可关联到 Admin Chat 会话的开放模块。
    /// </summary>
    public class AvailableModuleDto
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public bool IsRequired { get; set; }
    }

    /// <summary>
    /// 获取 AI 模型选项响应
    /// </summary>
    public class GetAiModelOptionsResponse
    {
        public bool AiKernelAvailable { get; set; }
        public List<AiModelOptionDto> Models { get; set; }
    }

    /// <summary>
    /// AI 模型选项
    /// </summary>
    public class AiModelOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDefault { get; set; }
    }

    #endregion
}
