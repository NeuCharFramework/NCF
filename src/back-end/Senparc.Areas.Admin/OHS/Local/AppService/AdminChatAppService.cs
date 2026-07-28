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

----------------------------------------------------------------*/
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
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
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly AdminChatSessionService _sessionService;
        private readonly AdminChatMessageService _messageService;
        private readonly AdminChatSessionModuleService _sessionModuleService;
        private readonly AdminChatAiService _chatAiService;
        private readonly IStringLocalizer<AdminResource> _localizer;
        private readonly IEventBus _eventBus;

        public AdminChatAppService(
            IServiceProvider serviceProvider,
            AdminChatSessionService sessionService,
            AdminChatMessageService messageService,
            AdminChatSessionModuleService sessionModuleService,
            AdminChatAiService chatAiService,
            IStringLocalizer<AdminResource> localizer) : base(serviceProvider)
        {
            _sessionService = sessionService;
            _messageService = messageService;
            _sessionModuleService = sessionModuleService;
            _chatAiService = chatAiService;
            _localizer = localizer;
            _eventBus = serviceProvider.GetService<IEventBus>();
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

                var title = string.IsNullOrEmpty(request.InitialMessage) 
                    ? _localizer["AdminChat.NewConversation"] 
                    : (request.InitialMessage.Length > 50 ? request.InitialMessage.Substring(0, 50) + "..." : request.InitialMessage);

                var session = await _sessionService.CreateSessionAsync(title, userId);

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

                if (!string.IsNullOrEmpty(request.InitialMessage))
                {
                    await _messageService.AddMessageAsync(
                        session.Id,
                        ChatMessageRoleType.User,
                        request.InitialMessage);

                    var (aiResponse, modelIdentifier) = await _chatAiService.GenerateResponseAsync(session.Id, userId, request.InitialMessage, request.AiModelId);
                    await _messageService.AddMessageAsync(
                        session.Id,
                        ChatMessageRoleType.Assistant,
                        aiResponse,
                        modelIdentifier);
                }

                logger.Append($"创建会话: SessionId={session.Id}, UserId={userId}");
                await PublishSyncEventAsync(userId, session.Id, "session-created");

                return new CreateSessionResponse
                {
                    SessionId = session.Id,
                    Title = session.Title
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

                return new GetSessionListResponse
                {
                    Sessions = sessions.Select(AdminChatSessionDto.CreateFromEntity).ToList(),
                    TotalCount = totalCount
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

                var (messages, _) = await _messageService.GetSessionMessagesAsync(sessionId);
                var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);

                var sessionDto = AdminChatSessionDto.CreateFromEntity(session);
                sessionDto.Messages = messages.Select(AdminChatMessageDto.CreateFromEntity).ToList();
                sessionDto.Modules = modules.Select(z => MapModuleDtoWithRegisterInfo(AdminChatSessionModuleDto.CreateFromEntity(z))).ToList();

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

        #endregion

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

                var userMessage = await _messageService.AddMessageAsync(
                    request.SessionId,
                    ChatMessageRoleType.User,
                    request.Content);

                await _sessionService.UpdateLastMessageTimeAsync(request.SessionId);

                var (aiResponse, modelIdentifier) = await _chatAiService.GenerateResponseAsync(request.SessionId, userId, request.Content, request.AiModelId);

                var assistantMessage = await _messageService.AddMessageAsync(
                    request.SessionId,
                    ChatMessageRoleType.Assistant,
                    aiResponse,
                    modelIdentifier);

                logger.Append($"发送消息: SessionId={request.SessionId}, MessageId={userMessage.Id}");
                await PublishSyncEventAsync(userId, request.SessionId, "messages-changed");

                return new SendMessageResponse
                {
                    UserMessage = AdminChatMessageDto.CreateFromEntity(userMessage),
                    AssistantMessage = AdminChatMessageDto.CreateFromEntity(assistantMessage)
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
        /// 设置消息反馈
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Put)]
        public async Task<StringAppResponse> SetMessageFeedbackAsync(int messageId, MessageFeedbackType feedback)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var success = await _messageService.SetMessageFeedbackAsync(messageId, feedback);
                if (!success)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.MessageNotFound"]);
                }

                logger.Append($"设置反馈: MessageId={messageId}, Feedback={feedback}");
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

                var modules = request.Modules.Select(m => (m.Uid, m.Name, m.Version ?? "")).ToList();
                await _sessionModuleService.AddModulesToSessionAsync(request.SessionId, modules);

                logger.Append($"添加模块: SessionId={request.SessionId}, Count={modules.Count}");
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
                        Name = "系统级 SenparcAiSetting",
                        Description = "使用 appsettings.json 中当前生效的默认 Chat 配置",
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
    }

    /// <summary>
    /// 获取会话列表响应
    /// </summary>
    public class GetSessionListResponse
    {
        public List<AdminChatSessionDto> Sessions { get; set; }
        public int TotalCount { get; set; }
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
        public AdminChatMessageDto UserMessage { get; set; }
        public AdminChatMessageDto AssistantMessage { get; set; }
    }

    /// <summary>
    /// 获取消息列表响应
    /// </summary>
    public class GetMessagesResponse
    {
        public List<AdminChatMessageDto> Messages { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 添加模块到会话请求
    /// </summary>
    public class AddModulesRequest
    {
        public int SessionId { get; set; }
        public List<ModuleInfo> Modules { get; set; }
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
