using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Enums;
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

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [IgnoreAuth]
    public class XncfModuleStartModel2 : BaseAdminPageModel
    {
        private readonly SysMenuService _sysMenuService;
        public Senparc.Ncf.Core.Models.DataBaseModel.XncfModule XncfModule { get; set; }
        //public Dictionary<IXncfFunction, List<FunctionParameterInfo>> FunctionParameterInfoCollection { get; set; } = new Dictionary<IXncfFunction, List<FunctionParameterInfo>>();

        XncfModuleService _xncfModuleService;
        IServiceProvider _serviceProvider;

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

        public XncfModuleStartModel2(IServiceProvider serviceProvider, XncfModuleService xncfModuleService, SysMenuService sysMenuService)
        {
            _serviceProvider = serviceProvider;
            _xncfModuleService = xncfModuleService;
            _sysMenuService = sysMenuService;
        }

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
                throw new Exception("模块未添加！");
            }

            module.UpdateState(toState);
            await _xncfModuleService.SaveObjectAsync(module).ConfigureAwait(false);
            base.SetMessager(MessageType.success, "状态变更成功！");
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
            var xncfRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == executeFuncParamDto2.XncfUid);

            if (xncfRegister == null)
            {
                return new JsonResult(new { success = false, msg = "模块未注册！" });
            }

            var xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == xncfRegister.Uid).ConfigureAwait(false);
            if (xncfModule == null)
            {
                return new JsonResult(new { success = false, msg = "当前模块未安装！" });
            }

            if (xncfModule.State != XncfModules_State.开放)
            {
                return new JsonResult(new { success = false, msg = $"当前模块状态为【{xncfModule.State}】,必须为【开放】状态的模块才可执行！\r\n此外，如果您强制执行此方法，也将按照未通过验证的程序集执行，因为您之前安装的版本可能已经被新的程序所覆盖。" });
            }

            FunctionBase function = null;

            foreach (var functionType in xncfRegister.Functions)
            {
                var fun = _serviceProvider.GetService(functionType) as FunctionBase;//如：Senparc.Xncf.ChangeNamespace.Functions.ChangeNamespace
                //var functionParameters = await function.GetFunctionParameterInfoAsync(_serviceProvider, false);
                if (fun.Name == executeFuncParamDto2.XncfFunctionName)
                {
                    function = fun;
                    break;
                }
            }

            if (function == null)
            {
                return new JsonResult(new { success = false, msg = "方法未匹配上！" });
            }

            var paras = SerializerHelper.GetObject(executeFuncParamDto2.XncfFunctionParams, function.FunctionParameterType) as IFunctionParameter;
            //var paras = function.GenerateParameterInstance();

            var result = function.Run(paras);

            var tempId = "Xncf-FunctionRun-" + Guid.NewGuid().ToString("n");
            //记录日志缓存
            if (!result.Log.IsNullOrEmpty())
            {
                var cache = _serviceProvider.GetObjectCacheStrategyInstance();
                await cache.SetAsync(tempId, result.Log, TimeSpan.FromMinutes(5));//TODO：可设置
            }

            var data = new { success = result.Success, msg = result.Message.HtmlEncode(), log = result.Log, exception = result.Exception?.Message, tempId = tempId };
            return new JsonResult(data);
        }

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
                return Content("日志文件不存在或已下载！");
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
                throw new Exception("模块未添加！");
            }

            //删除菜单
            Func<Task> uninstall = async () =>
            {
                //删除菜单
                SysPermissionService sysPermissionService = _serviceProvider.GetService<SysPermissionService>();
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


            IDictionary<(string key,string name, string description), List<FunctionParameterInfo>> functionParameterInfoCollection = new Dictionary<(string key, string name, string description), List<FunctionParameterInfo>>();


            try
            {
                if (Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(typeof(Register), out var functionGroup))
                {
                    foreach (var funtionBag in functionGroup.Values)
                    {
                        //var obj = GenerateParameterInstance();


                        ////预载入参数
                        //if (tryLoadData && obj is IFunctionParameterLoadDataBase loadDataParam)
                        //{
                        //    await loadDataParam.LoadData(serviceProvider);//载入参数
                        //}

                        var functionParameterType = funtionBag.MethodInfo.GetParameters().FirstOrDefault()?.ParameterType;
                        if (functionParameterType == null)
                        {
                            functionParameterType = typeof(FunctionAppRequestBase);
                        }

                        var obj = functionParameterType.Assembly.CreateInstance(functionParameterType.FullName) as FunctionAppRequestBase;


                        var props = functionParameterType.GetProperties();
                        ParameterType parameterType = ParameterType.Text;
                        List<FunctionParameterInfo> result = new List<FunctionParameterInfo>();
                        foreach (var prop in props)
                        {
                            SelectionList selectionList = null;
                            parameterType = ParameterType.Text;//默认为文本内容
                                                               //判断是否存在选项
                            if (prop.PropertyType == typeof(SelectionList))
                            {
                                var selections = prop.GetValue(obj, null) as SelectionList;
                                switch (selections.SelectionType)
                                {
                                    case SelectionType.DropDownList:
                                        parameterType = ParameterType.DropDownList;
                                        break;
                                    case SelectionType.CheckBoxList:
                                        parameterType = ParameterType.CheckBoxList;
                                        break;
                                    default:
                                        //TODO: throw
                                        break;
                                }
                                selectionList = selections;
                            }

                            var name = prop.Name;
                            string title = null;
                            string description = null;
                            var isRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;
                            var descriptionAttr = prop.GetCustomAttribute<DescriptionAttribute>();
                            if (descriptionAttr != null && descriptionAttr.Description != null)
                            {
                                //分割：名称||说明
                                var descriptionAttrArr = descriptionAttr.Description.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                                title = descriptionAttrArr[0];
                                if (descriptionAttrArr.Length > 1)
                                {
                                    description = descriptionAttrArr[1];
                                }
                            }
                            var systemType = prop.PropertyType.Name;

                            object value = null;
                            try
                            {
                                value = prop.GetValue(obj);
                            }
                            catch (Exception ex)
                            {
                                SenparcTrace.BaseExceptionLog(ex);
                            }

                            var functionParamInfo = new FunctionParameterInfo(name, title, description, isRequired, systemType, parameterType,
                                                        selectionList ?? new SelectionList(SelectionType.Unknown), value);
                            result.Add(functionParamInfo);
                        }

                        //TODO: 以上方法需要集成到基础库
                        var functionKey = funtionBag.Key;
                        functionParameterInfoCollection[(functionKey, funtionBag.FunctionRenderAttribute.Name, funtionBag.FunctionRenderAttribute.Description)] = result;
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
                    areaPageMenuItems = (xncfRegister as Ncf.Core.Areas.IAreaRegister)?.AareaPageMenuItems ?? new List<Ncf.Core.Areas.AreaPageMenuItem>(),
                    Interfaces = xncfRegister.GetType().GetInterfaces().Select(z => z.Name),
                    FunctionCount = xncfRegister.Functions.Count,
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
                        z.Key.name,
                        z.Key.description
                    },
                    z.Value
                }),
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
                throw new Exception("模块未添加！");
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
}
