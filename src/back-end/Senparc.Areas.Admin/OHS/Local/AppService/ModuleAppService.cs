using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.Areas.Admin.Pages;
using Senparc.Areas.Admin.Domain.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase.Threads;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Senparc.Areas.Admin.OHS.Local.PL.Module_GetItemResponse.Response_XncfRegister;
using static Senparc.Areas.Admin.OHS.Local.PL.Module_GetItemResponse.Response_XncfRegister.Response_XncfRegister_RegisteredThreadInfo;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    [BackendJwtAuthorize]
    public class ModuleAppService : LocalAppServiceBase
    {
        private readonly XncfModuleServiceExtension _xncfModuleServiceEx;

        private readonly XncfModuleService _xncfModuleService;

        public ModuleAppService(IServiceProvider serviceProvider, XncfModuleServiceExtension xncfModuleServiceEx, XncfModuleService xncfModuleService) : base(serviceProvider)
        {
            _xncfModuleServiceEx = xncfModuleServiceEx;
            _xncfModuleService = xncfModuleService;
        }

        /// <summary>
        /// 获取模块统计状态
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<Module_StatResponse>> StatAsync()
        {
            var response = await this.GetResponseAsync<AppResponseBase<Module_StatResponse>, Module_StatResponse>(async (response, logger) =>
            {
                //所有已安装模块
                var installedXncfModules = await _xncfModuleServiceEx.GetFullListAsync(z => true);
                //未安装或待升级模块
                var updateXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(installedXncfModules);
                //未安装货代升级模块的版本
                var newVersions = updateXncfRegisters.Select(z => _xncfModuleServiceEx.GetVersionDisplayName(installedXncfModules, z));
                //需要升级的版本号
                var newXncfCount = newVersions.Count(z => !z.Contains("->"));
                //全新未安装的版本号
                var updateVersionXncfCount = newVersions.Count() - newXncfCount;
                //安装后缺失的模块
                var xncfRegisterManager = new XncfRegisterManager(base.ServiceProvider);
                var missingXncfCount = installedXncfModules.Count(z => !XncfRegisterManager.RegisterList.Exists(r => r.Uid == z.Uid));

                var data = new Module_StatResponse()
                {
                    InstalledXncfCount = installedXncfModules.Count,
                    UpdateVersionXncfCount = updateVersionXncfCount,
                    NewXncfCount = newXncfCount,
                    MissingXncfCount = missingXncfCount
                };

                return data;
            });
            return response;
        }

        /// <summary>
        /// 获取已安装模块信息
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<List<XncfModuleDto>>> GetInstalledListAsync()
        {
            var response = await this.GetResponseAsync<AppResponseBase<List<XncfModuleDto>>, List<XncfModuleDto>>(async (response, logger) =>
            {
                //所有已安装模块
                var installedXncfModules = await _xncfModuleServiceEx.GetFullListAsync(z => true);
                var result = installedXncfModules.Select(z => _xncfModuleServiceEx.Mapper.Map<XncfModuleDto>(z)).ToList();
                return result;
            });
            return response;
        }


        /// <summary>
        /// 获取未安装或待更新（版本不同）模块信息
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<List<XncfModuleDto>>> GetUnInstalledListAsync()
        {
            var response = await this.GetResponseAsync<AppResponseBase<List<XncfModuleDto>>, List<XncfModuleDto>>(async (response, logger) =>
            {
                //所有已安装模块
                var installedXncfModules = await _xncfModuleServiceEx.GetFullListAsync(z => true);
                //未安装或待升级模块
                var updateXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(installedXncfModules);

                var uninstallOrUpdateXncfModules = updateXncfRegisters
                            .Select(z =>
                            {
                                var installedXncfModule = installedXncfModules.FirstOrDefault(m => m.Uid == z.Uid);
                                var xncfModuleId = installedXncfModule?.Id ?? default;
                                var version = xncfModuleId != default ? $"{installedXncfModule.Version} -> {z.Version}" : z.Version;
                                var state = installedXncfModule != null ? XncfModules_State.更新待审核 : XncfModules_State.关闭;
                                var xncfModuleDto = new XncfModuleDto(xncfModuleId, z.Name, z.Uid, z.MenuName, version, z.Description, null, false, null, z.Icon, state);
                                return xncfModuleDto;
                            }).ToList();

                //var result = updateXncfRegisters.Select(z => z as XncfRegisterBase).ToList();
                return uninstallOrUpdateXncfModules;
            });
            return response;
        }

        /// <summary>
        /// 安装模块
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<int>> InstallXncfModuleAsync(string uid)
        {
            var response = await this.GetResponseAsync<AppResponseBase<int>, int>(async (response, logger) =>
            {
                var result = await _xncfModuleServiceEx.InstallModuleAsync(uid);
                return 0;
            });
            return response;
        }



        #region 单个模块页面操作

        /// <summary>
        /// 获取单个模块信息
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<Module_GetItemResponse>> GetItemAsync(string uid)
        {
            return await this.GetResponseAsync<AppResponseBase<Module_GetItemResponse>, Module_GetItemResponse>(async (response, logger) =>
            {

                bool mustUpdate = false;
                if (uid.IsNullOrEmpty())
                {
                    throw new Exception("模块编号未提供！");
                }


                Ncf.Core.Models.DataBaseModel.XncfModule xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == uid).ConfigureAwait(false);

                if (xncfModule == null)
                {
                    throw new Exception("模块未添加！");
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
                    throw new Exception($"模块丢失或未加载（{XncfRegisterManager.RegisterList.Count}）！");
                }

                IDictionary<(string key, string name, string description), List<FunctionParameterInfo>> functionParameterInfoCollection = new Dictionary<(string key, string name, string description), List<FunctionParameterInfo>>();

                try
                {
                    if (Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(xncfRegister.GetType(), out var functionGroup))
                    {
                        //遍历某个 Register 下所有的方法      TODO：未来可添加分组
                        foreach (var funtionBag in functionGroup.Values)
                        {
                            var result = await FunctionHelper.GetFunctionParameterInfoAsync(base.ServiceProvider, funtionBag, true);

                            var functionKey = funtionBag.Key;
                            functionParameterInfoCollection[(functionKey, funtionBag.FunctionRenderAttribute.Name, funtionBag.FunctionRenderAttribute.Description)] = result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("模块读取失败", @$"模块：{xncfModule.Name} / {xncfModule.MenuName} / {xncfModule.Uid}
请尝试更新此模块后刷新页面！\r\n{ex.Message}\r\n{ex.StackTrace}");
                    mustUpdate = true;

                    throw new Exception($"模块读取失败，请尝试更新此模块后刷新页面！模块：{xncfModule.Name} / {xncfModule.MenuName} / {xncfModule.Uid}");
                }

                IEnumerable<KeyValuePair<ThreadInfo, Thread>> registeredThreadInfo = xncfRegister.RegisteredThreadInfo;

                var getItemResult = new Module_GetItemResponse()
                {
                    MustUpdate = mustUpdate,
                    XncfModule = _xncfModuleServiceEx.Mapper.Map<XncfModuleDto>(xncfModule),
                    XncfRegister = new Module_GetItemResponse.Response_XncfRegister()
                    {
                        AreaHomeUrl = xncfRegister.GetAreaHomeUrl(),
                        MenuName = xncfRegister.MenuName,
                        Icon = xncfRegister.Icon,
                        Version = xncfRegister.Version,
                        Uid = xncfRegister.Uid,
                        AreaPageMenuItems = (xncfRegister as Ncf.Core.Areas.IAreaRegister)?.AreaPageMenuItems ?? new List<Ncf.Core.Areas.AreaPageMenuItem>(),
                        Interfaces = xncfRegister.GetType().GetInterfaces().Select(z => z.Name).ToList(),
                        FunctionCount = functionParameterInfoCollection.Count,
                        RegisteredThreadInfo = xncfRegister.RegisteredThreadInfo.Select(z => new Response_XncfRegister_RegisteredThreadInfo()
                        {
                            Key = new RegisteredThreadInfo_Key()
                            {
                                Name = z.Key.Name,
                                StoryHtml = z.Key.StoryHtml
                            },
                            Value = new RegisteredThreadInfo_Value()
                            {
                                IsAlive = z.Value.IsAlive,
                                IsBackground = z.Value.IsAlive ? new bool?(z.Value.IsBackground) : null,
                                ThreadState = z.Value.IsAlive ? new ThreadState?(z.Value.ThreadState) : null,
                                ThreadStateStr = z.Value.IsAlive ? z.Value.ThreadState.ToString() : null
                            }
                        }),
                    },
                    //FunctionParameterInfoCollection = functionParameterInfoCollection.Select(z =>
                    //    new KeyValuePair<(string name, string description), List<FunctionParameterInfo>>(
                    //        (z.Key.name, z.Key.description), z.Value)
                    //    ).OrderBy(z => z.Key.name).ToList(),
                    FunctionParameterInfoCollection = functionParameterInfoCollection
                            .Select(z => new Module_GetItemResponse.Response_FunctionParameterInfoCollection()
                            {
                                Key = new Module_GetItemResponse.Response_FunctionParameterInfoCollection_Key(z.Key.name, z.Key.description),
                                Value = z.Value
                            }).OrderBy(z => z.Key.Name).ToList()
                };

                return getItemResult;
            });
        }


        /// <summary>
        /// 更改状态
        /// </summary>
        /// <param name="id">模块 ID</param>
        /// <param name="toState">切换到状态：关闭-0，开放-1，新增待审核-2，更新待审核-3</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Put)]
        public async Task<StringAppResponse> ChangeStateAsync(int id, XncfModules_State toState)
        {
            var response = await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var module = await _xncfModuleService.GetObjectAsync(z => z.Id == id).ConfigureAwait(false);

                if (module == null)
                {
                    throw new Exception("模块未添加！");
                }

                module.UpdateState(toState);
                await _xncfModuleService.SaveObjectAsync(module).ConfigureAwait(false);
                return "OK";
            });
            return response;
        }

        ///// <summary>
        ///// 获取日志
        ///// </summary>
        ///// <param name="tempId">日志ID</param>
        ///// <returns></returns>
        //public async Task<StringAppResponse> GetLogAsync(string tempId)
        //{
        //    var response = await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
        //    {
        //        var cache = _serviceProvider.GetObjectCacheStrategyInstance();
        //        var log = await cache.GetAsync<string>(tempId);
        //        if (log == null)
        //        {
        //            return Content("日志文件不存在或已下载！");
        //        }

        //        await cache.RemoveFromCacheAsync(tempId);

        //        return File(Encoding.UTF8.GetBytes(log), "text/plain", $"xncf-log-{tempId}.txt");
        //    });
        //    return response;
        //}

        /// <summary>
        /// 删除模块
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> DeleteAsync(int id)
        {
            var response = await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var module = await _xncfModuleService.GetObjectAsync(z => z.Id == id).ConfigureAwait(false);

                if (module == null)
                {
                    throw new Exception("模块未添加！");
                }

                //删除菜单
                Func<Task> uninstall = async () =>
                {
                    //删除菜单
                    SysPermissionService sysPermissionService = base.ServiceProvider.GetService<SysPermissionService>();
                    Ncf.Service.SysMenuService sysMenuService = base.ServiceProvider.GetService<Ncf.Service.SysMenuService>();
                    var menu = await sysMenuService.GetObjectAsync(z => z.Id == module.MenuId).ConfigureAwait(false);
                    if (menu != null)
                    {
                        //删除菜单
                        await sysMenuService.DeleteObjectAsync(menu).ConfigureAwait(false);
                        //删除权限数据
                        await sysPermissionService.DeleteAllAsync(z => z.PermissionId == menu.Id);
                        //更新菜单缓存                                                                                                                            
                        await sysMenuService.GetMenuDtoByCacheAsync(true).ConfigureAwait(false);
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
                    await register.UninstallAsync(base.ServiceProvider, uninstall).ConfigureAwait(false);
                }

                return "OK";
            });

            return response;
        }


        /// <summary>
        /// 提交信息，执行方法
        /// </summary>
        /// <param name="executeFuncParamDto2">提交参数</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<AppResponseBase<Module_RunFunctionResponse>> RunFunctionAsync([FromBody] ExecuteFuncParamDto2 executeFuncParamDto2)
        {
            return await this.GetResponseAsync<AppResponseBase<Module_RunFunctionResponse>, Module_RunFunctionResponse>(async (response, logger) =>
            {
                var xncfRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == executeFuncParamDto2.XncfUid);

                if (xncfRegister == null)
                {
                    response.Success = false;
                    response.ErrorMessage = "模块未注册！";
                    return null;
                }

                var xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == xncfRegister.Uid).ConfigureAwait(false);
                if (xncfModule == null)
                {
                    response.Success = false;
                    response.ErrorMessage = "当前模块未安装！";
                    return null;
                }

                if (xncfModule.State != XncfModules_State.开放)
                {
                    response.Success = false;
                    response.ErrorMessage = $"当前模块状态为【{xncfModule.State}】,必须为【开放】状态的模块才可执行！\r\n此外，如果您强制执行此方法，也将按照未通过验证的程序集执行，因为您之前安装的版本可能已经被新的程序所覆盖。";
                    return null;
                }

                FunctionRenderBag? rightFunctionBag = null;
                if (Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(xncfRegister.GetType(), out var functionGroup))
                {
                    foreach (var funtionBag in functionGroup.Values)
                    {
                        var funClass = base.ServiceProvider.GetService(funtionBag.MethodInfo.DeclaringType) as IAppService;
                        //var funMethod = funClass.GetType().GetMethod()

                        if (funtionBag.FunctionRenderAttribute.Name == executeFuncParamDto2.XncfFunctionName)
                        {
                            rightFunctionBag = funtionBag;
                            break;
                        }
                    }
                }

                if (rightFunctionBag == null)
                {
                    response.Success = false;
                    response.ErrorMessage = "方法未匹配上！";
                    return null;
                }

                var functionParameterType = rightFunctionBag.Value.MethodInfo.GetParameters().FirstOrDefault()?.ParameterType;
                if (functionParameterType == null)
                {
                    functionParameterType = typeof(FunctionAppRequestBase);
                }

                var paramCount = rightFunctionBag.Value.MethodInfo.GetParameters().Length;
                object[] paras = null;
                switch (paramCount)
                {
                    case 1:
                        var requestPara = SerializerHelper.GetObject(executeFuncParamDto2.XncfFunctionParams, functionParameterType) as IAppRequest;
                        paras = new[] { requestPara };
                        break;
                    case 0:
                        //不处理
                        break;
                    default:
                        {
                            response.Success = false;
                            response.ErrorMessage = "FunctionRender 只允许方法具有一个传入参数！";
                            return null;
                        }
                }

                var functionClass = base.ServiceProvider.GetService(rightFunctionBag.Value.MethodInfo.DeclaringType);
                var taskFunc = rightFunctionBag.Value.MethodInfo.Invoke(functionClass, paras) as Task; // Task<StringAppResponse>  as IAppResponse;
                await taskFunc.ConfigureAwait(false);


                //方案一：
                //参考：https://stackoverflow.com/questions/48033760/cast-taskt-to-taskobject-in-c-sharp-without-having-t/48033780
                var taskResult = (object)((dynamic)taskFunc).Result;
                var result = taskResult as IAppResponse;

                /* 方案二：
                var obj0 = rightFunctionBag.Value.MethodInfo.Invoke(functionClass, paras);
                var obj1 = taskFunc.GetType().GetMethod("GetAwaiter").Invoke(taskFunc, null);
                var obj2= obj1.GetType().GetMethod("GetResult").Invoke(obj1, null);
                var realResult = obj2 as IAppResponse;

                方案效率对比见：https://www.cnblogs.com/szw/p/dynamic-vs-reflect.html
                */

                //已经在 AppService 中记录
                //var tempId = "Xncf-FunctionRun-" + Guid.NewGuid().ToString("n");
                ////记录日志缓存
                //if (result.Data != null)
                //{
                //    var cache = _serviceProvider.GetObjectCacheStrategyInstance();
                //    await cache.SetAsync(tempId, result.Data.ToJson(), TimeSpan.FromMinutes(5));//TODO：可设置
                //}

                var data = new Module_RunFunctionResponse
                {
                    Msg = result.Data?.ToJson().HtmlEncode(),
                    Log = result.Data?.ToJson().HtmlEncode(),
                    Exception = result.ErrorMessage,
                    TempId = result.RequestTempId
                };
                return data;
            });
        }


        #endregion
    }
}
