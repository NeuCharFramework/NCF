/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Start.cshtml.cs
    文件功能描述：Start.cshtml.cs 相关实现


    创建标识：Senparc - 20241028

    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase.Threads;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Senparc.Areas.Admin;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Shared.Abstractions.ChatAgent;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [IgnoreAuth]
    [AdminAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
    public class XncfModuleStartModel(IServiceProvider serviceProvider, XncfModuleService xncfModuleService,
        Senparc.Ncf.Service.SysMenuService sysMenuService,
        NeuCharFunctionService neuCharFunctionService,
        NeuCharPivotService neuCharPivotService,
        NeuCharPivotFunctionService neuCharPivotFunctionService,
        NeuCharPivotLoopTaskService neuCharPivotLoopTaskService,
        NeuCharExecutionLogService neuCharExecutionLogService,
        NeuCharParameterProtector neuCharParameterProtector,
        IEventBusRequestClient eventBusRequestClient,
        IAdminWorkContextProvider adminWorkContextProvider) : BaseAdminPageModel(serviceProvider)
    {
        private readonly Senparc.Ncf.Service.SysMenuService _sysMenuService = sysMenuService;
        public Senparc.Ncf.Core.Models.DataBaseModel.XncfModule XncfModule { get; set; }
        //public Dictionary<IXncfFunction, List<FunctionParameterInfo>> FunctionParameterInfoCollection { get; set; } = new Dictionary<IXncfFunction, List<FunctionParameterInfo>>();

        XncfModuleService _xncfModuleService = xncfModuleService;
        IServiceProvider _serviceProvider = serviceProvider;
        private readonly IStringLocalizer<AdminResource> _localizer = serviceProvider.GetRequiredService<IStringLocalizer<AdminResource>>();
        private readonly NeuCharFunctionService _neuCharFunctionService = neuCharFunctionService;
        private readonly NeuCharPivotService _neuCharPivotService = neuCharPivotService;
        private readonly NeuCharPivotFunctionService _neuCharPivotFunctionService = neuCharPivotFunctionService;
        private readonly NeuCharPivotLoopTaskService _neuCharPivotLoopTaskService = neuCharPivotLoopTaskService;
        private readonly NeuCharExecutionLogService _neuCharExecutionLogService = neuCharExecutionLogService;
        private readonly NeuCharParameterProtector _neuCharParameterProtector = neuCharParameterProtector;
        private readonly IEventBusRequestClient _eventBusRequestClient = eventBusRequestClient;
        private readonly IAdminWorkContextProvider _adminWorkContextProvider = adminWorkContextProvider;

        public List<string> XncfModuleUpdateLog { get; set; }

        /// <summary>
        /// 获取当前模块的已注册线程信息
        /// </summary>
        public IEnumerable<KeyValuePair<ThreadInfo, Thread>> RegisteredThreadInfo { get; set; }

        /// <summary>
        /// 是否必须更新（常规读取失败）
        /// </summary>
        public bool MustUpdate { get; set; }

        public string Msg { get; set; }
        public object Obj { get; set; }

        public async Task OnGetAsync()
        {
            await Task.CompletedTask;
            //            if (uid.IsNullOrEmpty())
            //            {
            //                throw new Exception("模块编号未提供！");
            //            }


            //            XncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == uid).ConfigureAwait(false);

            //            if (XncfModule == null)
            //            {
            //                throw new Exception("模块未添加！");
            //            }

            //            if (!XncfModule.UpdateLog.IsNullOrEmpty())
            //            {
            //                XncfModuleUpdateLog = XncfModule.UpdateLog
            //                    .Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            //                    .ToList();
            //            }
            //            else
            //            {
            //                XncfModuleUpdateLog = new List<string>();
            //            }

            //            XncfRegister = Senparc.Ncf.XncfBase.Register.RegisterList.FirstOrDefault(z => z.Uid == uid);
            //            if (XncfRegister == null)
            //            {
            //                throw new Exception($"模块丢失或未加载（{Senparc.Ncf.XncfBase.Register.RegisterList.Count}）！");
            //            }

            //            try
            //            {
            //                foreach (var functionType in XncfRegister.Functions)
            //                {
            //                    var function = _serviceProvider.GetService(functionType) as FunctionBase;//如：Senparc.Xncf.ChangeNamespace.Functions.ChangeNamespace
            //                    FunctionParameterInfoCollection[function] = await function.GetFunctionParameterInfoAsync(_serviceProvider, true);
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                SenparcTrace.SendCustomLog("模块读取失败", @$"模块：{XncfModule.Name} / {XncfModule.MenuName} / {XncfModule.Uid}
            //请尝试更新此模块后刷新页面！");
            //                MustUpdate = true;
            //            }

            //            RegisteredThreadInfo = XncfRegister.RegisteredThreadInfo;
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="toState"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetChangeStateAsync(int id, XncfModules_State toState)
        {
            var module = await _xncfModuleService.GetObjectAsync(z => z.Id == id).ConfigureAwait(false);

            if (module == null)
            {
                throw new Exception(_localizer["Xncf.ModuleNotAdded"]);
            }

            module.UpdateState(toState);
            await _xncfModuleService.SaveObjectAsync(module).ConfigureAwait(false);
            base.SetMessager(MessageType.success, _localizer["Xncf.StateChangedSuccess"]);
            return RedirectToPage("Start", new { uid = module.Uid });
        }

        /// <summary>
        /// 提交信息，执行方法
        /// </summary>
        /// <param name="xncfUid"></param>
        /// <param name="xncfFunctionName"></param>
        /// <param name="xncfFunctionParams"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostRunFunctionAsync([FromBody] ExecuteFuncParamDto2 executeFuncParamDto2)
        {
            var result = await _neuCharFunctionService.ExecuteAsync(
                executeFuncParamDto2.XncfUid,
                executeFuncParamDto2.XncfFunctionName,
                executeFuncParamDto2.XncfFunctionParams,
                HttpContext.RequestAborted).ConfigureAwait(false);
            var returnData = result.Data is string stringData
                ? stringData.HtmlEncode()
                : result.Data?.ToJson().HtmlEncode();

            var data = new
            {
                success = result.Success,
                msg = returnData,
                log = returnData,
                exception = result.ErrorMessage,
                tempId = result.RequestTempId
            };
            return new JsonResult(data);
        }

        public async Task<IActionResult> OnGetNeuCharPivotAsync(string uid)
        {
            var snapshot = await _neuCharPivotService.GetSnapshotAsync(uid, HttpContext.RequestAborted)
                .ConfigureAwait(false);
            return Ok(snapshot == null ? null : ToPivotResponse(snapshot));
        }

        public async Task<IActionResult> OnPostGenerateNeuCharPivotAsync(
            [FromBody] GenerateNeuCharPivotRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.XncfUid))
            {
                return BadRequest("模块 UID 不能为空。");
            }
            if (request.UserRequirement?.Length > 10_000)
            {
                return BadRequest("生成要求不能超过 10000 个字符。");
            }

            var current = await _neuCharPivotService.GetSnapshotAsync(
                request.XncfUid,
                HttpContext.RequestAborted).ConfigureAwait(false);
            var operation = current == null
                ? ChatAgentOperation.GenerateNeuCharPivot
                : ChatAgentOperation.RefineNeuCharPivot;
            var adminUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
            var eventRequest = new ChatAgentRequestEvent(
                operation,
                Register.ModuleUid,
                request.XncfUid,
                adminUserId,
                request.AiModelId,
                request.UserRequirement,
                current?.Configuration.LayoutSchemaJson,
                current?.Configuration.ChatSessionId);
            ChatAgentResponseEvent response;
            try
            {
                response = await _eventBusRequestClient.RequestAsync<ChatAgentResponseEvent>(
                    eventRequest,
                    TimeSpan.FromMinutes(3),
                    HttpContext.RequestAborted).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                return StatusCode(504, "ChatAgent 生成超时，请稍后重试；本次异常会保留在服务端日志中。");
            }
            if (!response.Success)
            {
                return BadRequest(response.ErrorMessage ?? response.Message);
            }

            var snapshot = await _neuCharPivotService.GetSnapshotAsync(
                request.XncfUid,
                HttpContext.RequestAborted).ConfigureAwait(false);
            return Ok(ToPivotResponse(snapshot));
        }

        public async Task<IActionResult> OnPostSaveLoopTaskAsync([FromBody] SaveLoopTaskRequest request)
        {
            if (request == null || request.FunctionId <= 0 || request.ParametersJson?.Length > 1_000_000)
            {
                return BadRequest("Loop Task 请求无效，参数不能超过 1000000 个字符。");
            }
            var function = await _neuCharPivotFunctionService.GetObjectAsync(z => z.Id == request.FunctionId)
                .ConfigureAwait(false);
            if (function == null || !function.Visible)
            {
                return BadRequest("NeuCharPivot Function 不存在。");
            }

            var catalog = await _neuCharFunctionService.GetCatalogAsync(
                function.ModuleUid,
                true,
                HttpContext.RequestAborted).ConfigureAwait(false);
            var descriptor = catalog.FirstOrDefault(z => string.Equals(
                z.FunctionKey,
                function.FunctionKey,
                StringComparison.OrdinalIgnoreCase));
            if (descriptor == null)
            {
                return BadRequest("Function 在当前模块版本中已不存在。");
            }
            if (request.Enabled && !descriptor.ModuleAvailable)
            {
                return BadRequest("模块未安装、未加载或未开启，不能启用 Loop Task。");
            }
            var task = await _neuCharPivotLoopTaskService.GetObjectAsync(z => z.FunctionId == function.Id)
                .ConfigureAwait(false);
            var secretNames = descriptor.Parameters
                .Where(z => z.ParameterType == ParameterType.Password)
                .Select(z => z.Name)
                .ToArray();
            string plainParameters;
            string protectedParameters;
            try
            {
                plainParameters = _neuCharParameterProtector.MergeWithExisting(
                    request.ParametersJson,
                    task?.ParametersJson,
                    secretNames);
                protectedParameters = _neuCharParameterProtector.Protect(plainParameters, secretNames);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            var validationError = NeuCharFunctionService.ValidateRequiredParameters(
                descriptor.Parameters,
                plainParameters);
            if (request.Enabled && validationError != null)
            {
                return BadRequest(validationError);
            }

            task ??= new NeuCharPivotLoopTask(
                function.Id,
                _adminWorkContextProvider.GetAdminWorkContext().AdminUserId);
            task.Configure(request.IntervalSeconds, protectedParameters, request.Enabled, request.UseNeuBell);
            await _neuCharPivotLoopTaskService.SaveObjectAsync(task).ConfigureAwait(false);
            return Ok(new
            {
                task.Id,
                task.FunctionId,
                task.IntervalSeconds,
                task.Enabled,
                task.UseNeuBell,
                task.NextRunAt,
                task.LastRunAt,
                task.LastSucceeded,
                task.LastError
            });
        }

        public async Task<IActionResult> OnPostRunPivotFunctionAsync([FromBody] RunPivotFunctionRequest request)
        {
            if (request == null || request.FunctionId <= 0)
            {
                return BadRequest("NeuCharPivot Function 请求无效。");
            }
            var function = await _neuCharPivotFunctionService.GetObjectAsync(z => z.Id == request.FunctionId)
                .ConfigureAwait(false);
            if (function == null || !function.Visible)
            {
                return BadRequest("NeuCharPivot Function 不存在或已失效。");
            }

            var log = new NeuCharExecutionLog(
                "pivot",
                function.Id,
                function.ModuleUid,
                function.FunctionKey,
                function.FunctionName,
                $"pivot-{Guid.NewGuid():N}");
            await _neuCharExecutionLogService.SaveObjectAsync(log).ConfigureAwait(false);
            var result = await _neuCharFunctionService.ExecuteAsync(
                function.ModuleUid,
                function.FunctionKey,
                request.ParametersJson,
                HttpContext.RequestAborted).ConfigureAwait(false);
            log.Complete(result.Success, result.Data?.ToString(), result.ErrorMessage);
            await _neuCharExecutionLogService.SaveObjectAsync(log).ConfigureAwait(false);
            return Ok(result);
        }

        private object ToPivotResponse(NeuCharPivotSnapshot snapshot) => new
        {
            configuration = new
            {
                snapshot.Configuration.Id,
                snapshot.Configuration.ModuleUid,
                snapshot.Configuration.Name,
                snapshot.Configuration.UserRequirement,
                snapshot.Configuration.LayoutSchemaJson,
                snapshot.Configuration.AiModelId,
                snapshot.Configuration.ChatSessionId,
                snapshot.Configuration.Revision,
                snapshot.Configuration.LastGeneratedAt,
                snapshot.Configuration.LastError
            },
            functions = snapshot.Functions.Select(function => new
            {
                function.Id,
                function.ModuleUid,
                function.FunctionKey,
                function.FunctionName,
                function.Description,
                function.UiSchemaJson,
                function.DefaultParametersJson,
                function.ModuleVersion,
                function.Sort,
                function.Visible,
                available = snapshot.FunctionAvailability.TryGetValue(function.Id, out var available) && available,
                loopTask = snapshot.LoopTasks.TryGetValue(function.Id, out var task) ? new
                {
                    task.Id,
                    task.IntervalSeconds,
                    task.Enabled,
                    task.UseNeuBell,
                    task.NextRunAt,
                    task.LastRunAt,
                    task.LastSucceeded,
                    task.LastError
                } : null
            }),
            snapshot.ModuleAvailable,
            snapshot.ModuleState
        };

        /// <summary>
        /// 获取日志
        /// </summary>
        /// <param name="tempId"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetLogAsync(string tempId)
        {
            var cache = _serviceProvider.GetObjectCacheStrategyInstance();
            var log = await cache.GetAsync<string>(tempId);
            if (log == null)
            {
                return Content(_localizer["Xncf.LogFileNotFoundOrDownloaded"]);
            }

            await cache.RemoveFromCacheAsync(tempId);

            return File(Encoding.UTF8.GetBytes(log), "text/plain", $"xncf-log-{tempId}.txt");
        }

        /// <summary>
        /// 删除模块
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var module = await _xncfModuleService.GetObjectAsync(z => z.Id == id).ConfigureAwait(false);

            if (module == null)
            {
                throw new Exception(_localizer["Xncf.ModuleNotAdded"]);
            }

            //删除菜单
            Func<Task> uninstall = async () =>
            {
                //删除菜单
                Senparc.Ncf.Service.SysRolePermissionService sysPermissionService =
                    _serviceProvider.GetService<Senparc.Ncf.Service.SysRolePermissionService>();
                var menu = await _sysMenuService.GetObjectAsync(z => z.Id == module.MenuId).ConfigureAwait(false);
                if (menu != null)
                {
                    //删除菜单
                    await _sysMenuService.DeleteObjectAsync(menu).ConfigureAwait(false);
                    //删除权限数据
                    await sysPermissionService.DeleteAllAsync(_ => _.PermissionId == menu.Id);
                    //更新菜单缓存                                                                                                                            
                    await _sysMenuService.GetMenuDtoByCacheAsync(true).ConfigureAwait(false);
                }
                await _xncfModuleService.DeleteObjectAsync(module).ConfigureAwait(false);
            };


            //尝试从已加载的模块中执行删除过程
            var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == module.Uid);
            if (register == null)
            {
                //直接删除，如dll已经不存在，可能引发此问题，只能在当前系统内直接执行删除
                await uninstall().ConfigureAwait(false);
            }
            else
            {
                await register.UninstallAsync(_serviceProvider, uninstall).ConfigureAwait(false);
            }

            return Ok(true);
        }


        /// <summary>
        /// handler=Detail
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetDetailAsync(string uid)
        {
            bool mustUpdate = false;
            if (uid.IsNullOrEmpty())
            {
                throw new Exception(_localizer["Xncf.ModuleIdNotProvided"]);
            }


            Ncf.Core.Models.DataBaseModel.XncfModule xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == uid).ConfigureAwait(false);

            if (xncfModule == null)
            {
                throw new Exception(_localizer["Xncf.ModuleNotAdded"]);
            }
            IEnumerable<string> xncfModuleUpdateLog = new List<string>();
            if (!xncfModule.UpdateLog.IsNullOrEmpty())
            {
                xncfModuleUpdateLog = xncfModule.UpdateLog
                    .Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }

            IXncfRegister xncfRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == uid);
            if (xncfRegister == null)
            {
                throw new Exception(_localizer["Xncf.ModuleMissingOrNotLoaded", XncfRegisterManager.RegisterList.Count]);
            }

            IDictionary<(string key, string name, string description), List<FunctionParameterInfo>> functionParameterInfoCollection = new Dictionary<(string key, string name, string description), List<FunctionParameterInfo>>();

            try
            {
                if (Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(xncfRegister.GetType(), out var functionGroup))
                {
                    //遍历某个 Register 下所有的方法      TODO：未来可添加分组
                    foreach (var functionBag in functionGroup.Values)
                    {
                        try
                        {
                            var result = await FunctionHelper.GetFunctionParameterInfoAsync(this._serviceProvider, functionBag, true);

                            var functionKey = functionBag.Key;
                            functionParameterInfoCollection[(functionKey, functionBag.FunctionRenderAttribute.Name, functionBag.FunctionRenderAttribute.Description)] = result;
                        }
                        catch (Exception ex)
                        {
                            SenparcTrace.BaseExceptionLog(ex);
                            throw new Exception(_localizer["Xncf.FunctionLoadError", functionBag.Key]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SenparcTrace.SendCustomLog("模块读取失败", @$"模块：{XncfModule?.Name} / {XncfModule?.MenuName} / {XncfModule?.Uid}
请尝试更新此模块后刷新页面！\r\n{ex.Message}\r\n{ex.StackTrace}");
                mustUpdate = true;
                //TODO:页面上需要给提示
            }

            IEnumerable<KeyValuePair<ThreadInfo, Thread>> registeredThreadInfo = xncfRegister.RegisteredThreadInfo;
            return Ok(new
            {
                mustUpdate,
                xncfModule,
                xncfModuleUpdateLog,
                xncfRegister = new
                {
                    AreaHomeUrl = xncfRegister.GetAreaHomeUrl(),
                    xncfRegister.MenuName,
                    xncfRegister.Icon,
                    xncfRegister.Version,
                    xncfRegister.Uid,
                    areaPageMenuItems = (xncfRegister as Ncf.Core.Areas.IAreaRegister)?.AreaPageMenuItems ?? new List<Ncf.Core.Areas.AreaPageMenuItem>(),
                    Interfaces = xncfRegister.GetType().GetInterfaces().Select(z => z.Name),
                    FunctionCount = functionParameterInfoCollection.Count,
                    registeredThreadInfo = xncfRegister.RegisteredThreadInfo.Select(z => new
                    {
                        Key = new
                        {
                            z.Key.Name,
                            z.Key.StoryHtml
                        },
                        Value = new
                        {
                            z.Value.IsAlive,
                            IsBackground = z.Value.IsAlive ? new bool?(z.Value.IsBackground) : null,
                            ThreadState = z.Value.IsAlive ? new ThreadState?(z.Value.ThreadState) : null,
                            ThreadStateStr = z.Value.IsAlive ? z.Value.ThreadState.ToString() : null
                        }
                    })
                },
                functionParameterInfoCollection = functionParameterInfoCollection
                .Select(z => new
                {
                    Key = new
                    {
                        functionKey = z.Key.key,
                        z.Key.name,
                        z.Key.description
                    },
                    z.Value
                }).OrderBy(z => z.Key.name),
                registeredThreadInfo = registeredThreadInfo.Select(z => new
                {
                    Key = new
                    {
                        z.Key.Name,
                        z.Key.StoryHtml
                    },
                    Value = new
                    {
                        z.Value.IsAlive,
                        IsBackground = z.Value.IsAlive ? new bool?(z.Value.IsBackground) : null,
                        ThreadState = z.Value.IsAlive ? new ThreadState?(z.Value.ThreadState) : null,
                        ThreadStateStr = z.Value.IsAlive ? z.Value.ThreadState.ToString() : null
                    }
                })
            });
        }

        /// <summary>
        /// handler=ChangeStateAjax
        /// </summary>
        /// <param name="id"></param>
        /// <param name="toState"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetChangeStateAjaxAsync(int id, XncfModules_State toState)
        {
            var module = await _xncfModuleService.GetObjectAsync(z => z.Id == id).ConfigureAwait(false);

            if (module == null)
            {
                throw new Exception(_localizer["Xncf.ModuleNotAdded"]);
            }

            module.UpdateState(toState);
            await _xncfModuleService.SaveObjectAsync(module).ConfigureAwait(false);
            return Ok(true);
        }
    }

    public class ExecuteFuncParamDto2
    {
        [Required]
        public string XncfUid { get; set; }
        [Required]
        public string XncfFunctionName { get; set; }
        [Required]
        public string XncfFunctionParams { get; set; }
    }

    public sealed class GenerateNeuCharPivotRequest
    {
        [Required]
        public string XncfUid { get; set; }
        public string UserRequirement { get; set; }
        public int AiModelId { get; set; }
    }

    public sealed class SaveLoopTaskRequest
    {
        public int FunctionId { get; set; }
        public int IntervalSeconds { get; set; } = 300;
        public string ParametersJson { get; set; } = "{}";
        public bool Enabled { get; set; }
        public bool UseNeuBell { get; set; }
    }

    public sealed class RunPivotFunctionRequest
    {
        public int FunctionId { get; set; }
        public string ParametersJson { get; set; } = "{}";
    }
}
