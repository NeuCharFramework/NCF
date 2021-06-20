using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Extensions;
using Senparc.NeuChar;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Senparc.Xncf.WeixinManager.Services
{
    public class WeixinApiService2
    {
        private static Dictionary<PlatformType, Dictionary<string, ApiBindInfo>> _apiCollection = new Dictionary<PlatformType, Dictionary<string, ApiBindInfo>>();

        public static Dictionary<PlatformType, Assembly> WeixinApiAssemblyCollection { get; set; } = new Dictionary<PlatformType, Assembly>();

        public static Dictionary<PlatformType, string> WeixinApiAssemblyNames { get; private set; } = new Dictionary<PlatformType, string>(); //= "WeixinApiAssembly";
        public static Dictionary<PlatformType, string> WeixinApiAssemblyVersions { get; private set; } = new Dictionary<PlatformType, string>(); //= "WeixinApiAssembly";


        public WeixinApiService2()
        {
            //测试时关闭部分模块
            WeixinApiAssemblyNames[PlatformType.WeChat_OfficialAccount] = $"NeuCharDocApi.{PlatformType.WeChat_OfficialAccount}";
            WeixinApiAssemblyNames[PlatformType.WeChat_MiniProgram] = $"NeuCharDocApi.{PlatformType.WeChat_MiniProgram}";
            WeixinApiAssemblyNames[PlatformType.WeChat_Open] = $"NeuCharDocApi.{PlatformType.WeChat_Open}";
            WeixinApiAssemblyNames[PlatformType.WeChat_Work] = $"NeuCharDocApi.{PlatformType.WeChat_Work}";
        }

        /// <summary>
        /// 控制台打印日志
        /// </summary>
        /// <param name="msg"></param>
        public static void WriteLog(string msg)
        {
            Console.WriteLine($"{SystemTime.Now.ToString("yyyy-MM-dd HH:ss:mm.ffff")}\t\t{msg}");
        }

        /// <summary>
        /// 获取全局统一 docName
        /// </summary>
        /// <param name="platformType"></param>
        /// <returns></returns>
        public static string GetDocName(PlatformType platformType)
        {
            return $"{platformType}-v{WeixinApiAssemblyVersions[platformType]}";
        }

        /// <summary>
        /// 创建动态 WebApi
        /// </summary>
        public void BuildWebApi()
        {
            //预载入程序集，确保在下一步 RegisterApiBind() 可以顺利读取所有接口
            var preLoad = typeof(Senparc.Weixin.MP.AdvancedAPIs.AddGroupResult).ToString() != null
                && typeof(Senparc.Weixin.WxOpen.AdvancedAPIs.CustomApi).ToString() != null
                && typeof(Senparc.Weixin.Open.AccountAPIs.AccountApi).ToString() != null
                && typeof(Senparc.Weixin.TenPay.V3.TenPayV3).ToString() != null
                && typeof(Senparc.Weixin.Work.AdvancedAPIs.AppApi).ToString() != null;

            //确保 ApiBind 已经执行
            Senparc.NeuChar.Register.RegisterApiBind(preLoad);//参数为 true，确保重试绑定成功

            var weixinApis = Senparc.NeuChar.ApiBind.ApiBindInfoCollection.Instance.GetGroupedCollection();
            WriteLog($"get weixinApis groups: {weixinApis.Count()}");

            var dt1 = SystemTime.Now;
            WriteLog("==== Begin BuildWebApi ====");


            foreach (var apiGroup in weixinApis)
            {
                if (apiGroup.Count() == 0 || !WeixinApiAssemblyNames.ContainsKey(apiGroup.Key))
                {
                    WriteLog($"apiGroup 不存在可用对象: {apiGroup.Key}");
                    continue;
                }

                var groupStartTime = SystemTime.Now;

                var assembleName = WeixinApiAssemblyNames[apiGroup.Key];
                var xmlFileName = $"{apiGroup.First().Value.MethodInfo.DeclaringType.Assembly.GetName().Name}.xml";

                XDocument document = XDocument.Load($"App_Data/ApiDocXml/{xmlFileName}");
                var root = document.Root;
                WriteLog("Document Root Name:" + root.Name);
                root.Element("assembly").Element("name").Value = assembleName;
                var docMembers = root.Element("members").Elements("member");
                WriteLog($"find docMembers:{docMembers.Count()}");


                #region 动态创建代码
                //动态创建程序集
                AssemblyName dynamicApiAssemblyName = new AssemblyName(assembleName); //Assembly.GetExecutingAssembly().GetName();// new AssemblyName("DynamicAssembly");
                AppDomain currentDomain = Thread.GetDomain();
                AssemblyBuilder dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(dynamicApiAssemblyName, AssemblyBuilderAccess.RunAndCollect);

                //动态创建模块
                ModuleBuilder mb = dynamicAssembly.DefineDynamicModule(dynamicApiAssemblyName.Name);

                //储存 API
                _apiCollection[apiGroup.Key] = new Dictionary<string, ApiBindInfo>(apiGroup);
                var keyName = apiGroup.Key.ToString();//不要随意改规则，全局需要保持一致

                WriteLog($"search key: {apiGroup.Key} -> {keyName}");

                //动态创建类 XXController
                var controllerName = $"{keyName}Controller";
                TypeBuilder tb = mb.DefineType(controllerName, TypeAttributes.Public, typeof(Controller));
                //ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
                //多个Get参数放在同一个Controller中可能发生问题：NotSupportedException: HTTP method "GET" & path "wxapi/WeChat_OfficialAccount/CommonApi_CreateMenu" overloaded by actions - WeChat_OfficialAccountController.CommonApi_CreateMenu (WeixinApiAssembly),WeChat_OfficialAccountController.CommonApi_CreateMenu (WeixinApiAssembly). Actions require unique method/path combination for OpenAPI 3.0. Use ConflictingActionsResolver as a workaround

                /*以下方法如果遇到参数含有 IEnumerable<T> 参数，会抛出异常：
                 * InvalidOperationException: Action 'WeChat_MiniProgramController.TcbApi_UpdateIndex (NeuCharDocApi.WeChat_MiniProgram)' has more than one parameter that was specified or inferred as bound from request body. Only one parameter per action may be bound from body. Inspect the following parameters, and use 'FromQueryAttribute' to specify bound from query, 'FromRouteAttribute' to specify bound from route, and 'FromBodyAttribute' for parameters to be bound from body:
                IEnumerable<CreateIndex> create_indexes
                IEnumerable<DropIndex> drop_indexes
                 */
                //var t = typeof(ApiControllerAttribute);
                //tb.SetCustomAttribute(new CustomAttributeBuilder(t.GetConstructor(new Type[0]), new object[0]));


                //暂时取消登录验证  —— Jeffrey Su 2021.06.18
                //var t_0 = typeof(AuthorizeAttribute);
                //tb.SetCustomAttribute(new CustomAttributeBuilder(t_0.GetConstructor(new Type[0]), new object[0]));


                var t2 = typeof(RouteAttribute);
                tb.SetCustomAttribute(new CustomAttributeBuilder(t2.GetConstructor(new Type[] { typeof(string) }), new object[] { $"/wxapi/{keyName}" }));

                //添加Controller级别的分类（暂时无效果）

                var t2_0 = typeof(SwaggerOperationAttribute);
                var t2_0_tagName = new[] { keyName };
                var t2_0_tagAttrBuilder = new CustomAttributeBuilder(t2_0.GetConstructor(new Type[] { typeof(string), typeof(string) }),
                    new object[] { (string)null, (string)null },
                    new[] { t2_0.GetProperty("Tags") }, new[] { t2_0_tagName });
                tb.SetCustomAttribute(t2_0_tagAttrBuilder);

                ////添加返回类型标签 https://docs.microsoft.com/zh-cn/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-2.2&tabs=visual-studio
                //var t2_1 = typeof(ProducesAttribute);
                //tb.SetCustomAttribute(new CustomAttributeBuilder(t2_1.GetConstructor(new Type[] { typeof(string), typeof(string[]) }), new object[] { "application/json", (string[])null }));

                ////添加针对Controller的GroupName
                //var t2_2 = typeof(ApiExplorerSettingsAttribute);
                //var t2_2_groupName = apiGroup.Key.ToString();
                //var t2_2_tagAttrBuilder = new CustomAttributeBuilder(t2_2.GetConstructor(new Type[0]), new object[0],
                //    new[] { t2_0.GetProperty("GroupName") }, new[] { t2_2_groupName });
                //tb.SetCustomAttribute(t2_2_tagAttrBuilder);

                var filterList = apiGroup.Where(z => !z.Value.ApiBindAttribute.Name.EndsWith("Async")
                                         && !z.Value.MethodInfo.GetParameters().Any(p => p.IsOut)
                                         && z.Value.MethodInfo.ReturnType != typeof(Task<>)
                                         && z.Value.MethodInfo.ReturnType != typeof(void)
                                         && !z.Value.MethodInfo.IsGenericMethod //SemanticApi.SemanticSend 是泛型方法

                                         //临时过滤 IEnumerable 对象   —— Jeffrey Su 2021.06.17
                                         && !z.Value.MethodInfo.GetParameters().Any(z => z.ParameterType.Name.Contains("IEnumerable") || z.ParameterType.Name.Contains("IList`1"))

                                         )
                                    .OrderBy(z => z.Value.ApiBindAttribute.Name)
                                    .ToList();

                //把 CommonApi 提前到头部
                Func<KeyValuePair<string, ApiBindInfo>, bool> funcCommonApi = z => z.Value.ApiBindAttribute.Name.StartsWith("CommonApi.");
                var commonApiList = filterList.Where(z => funcCommonApi(z)).ToList();
                filterList.RemoveAll(z => funcCommonApi(z));
                filterList.InsertRange(0, commonApiList);

                int apiIndex = 0;
                List<string> apiMethodName = new List<string>();

                foreach (var apiBindInfo in filterList)
                {
                    try
                    {
                        apiIndex++;

                        if (apiIndex > 9999)//200-250
                        {
                            continue;//用于小范围分析
                        }

                        //定义版本号
                        if (!WeixinApiAssemblyVersions.ContainsKey(apiGroup.Key))
                        {
                            WeixinApiAssemblyVersions[apiGroup.Key] = apiBindInfo.Value.MethodInfo.DeclaringType.Assembly.GetName().Version.ToString(3);
                        }

                        //当前方法名称
                        var methodName = apiBindInfo.Value.ApiBindAttribute.Name.Replace(".", "_").Replace("-", "_").Replace("/", "_");
                        var apiGroupName = apiBindInfo.Value.ApiBindAttribute.Name.Split('.')[0];
                        var indexOfApiGroupDot = apiBindInfo.Value.ApiBindAttribute.Name.IndexOf(".");
                        var apiName = apiBindInfo.Value.ApiBindAttribute.Name.Substring(indexOfApiGroupDot + 1, apiBindInfo.Value.ApiBindAttribute.Name.Length - indexOfApiGroupDot - 1);

                        //确保名称不会有重复
                        while (apiMethodName.Contains(methodName))
                        {
                            methodName += "0";
                            apiName += "0";
                        }
                        apiMethodName.Add(methodName);

                        //当前 API 的 MethodInfo
                        MethodInfo apiMethodInfo = apiBindInfo.Value.MethodInfo;
                        //当前 API 的所有参数信息
                        var parameters = apiMethodInfo.GetParameters();

                        //WriteLog($"\t search API[{apiIndex}]: {keyName} > {apiBindInfo.Key} -> {methodName} \t\t Parameters Count: {parameters.Count()}\t\t");


                        MethodBuilder setPropMthdBldr =
                            tb.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Virtual,
                            apiMethodInfo.ReturnType, //返回类型
                            parameters.Select(z => z.ParameterType).ToArray()//输入参数
                            );

                        //Controller已经使用过一次SwaggerOperationAttribute
                        var t2_3 = typeof(SwaggerOperationAttribute);
                        var tagName = new[] { $"{keyName}:{apiGroupName}" };
                        var tagAttrBuilder = new CustomAttributeBuilder(t2_3.GetConstructor(new Type[] { typeof(string), typeof(string) }),
                            new object[] { (string)null, (string)null },
                            new[] { t2_3.GetProperty("Tags") }, new[] { tagName });
                        setPropMthdBldr.SetCustomAttribute(tagAttrBuilder);
                        //其他Method排序方法参考：https://stackoverflow.com/questions/34175018/grouping-of-api-methods-in-documentation-is-there-some-custom-attribute


                        //[Route("/wxapi/...", Name="xxx")]
                        var t2_4 = typeof(RouteAttribute);
                        //var routeName = apiBindInfo.Value.ApiBindAttribute.Name.Split('.')[0];
                        var apiPath = $"/wxapi/{keyName}/{apiGroupName}/{apiName}";
                        var routeAttrBuilder = new CustomAttributeBuilder(t2_4.GetConstructor(new Type[] { typeof(string) }),
                            new object[] { apiPath }/*, new[] { t2_2.GetProperty("Name") }, new[] { routeName }*/);
                        setPropMthdBldr.SetCustomAttribute(routeAttrBuilder);
                        WriteLog($"added Api path: {apiPath}");


                        //[HttpGet]
                        var t3 = typeof(HttpGetAttribute);
                        setPropMthdBldr.SetCustomAttribute(new CustomAttributeBuilder(t3.GetConstructor(new Type[0]), new object[0]));

                        //用户限制  ——  Jeffrey Su 2021.06.18
                        //var t4 = typeof(UserAuthorizeAttribute);//[UserAuthorize("UserOnly")]
                        //setPropMthdBldr.SetCustomAttribute(new CustomAttributeBuilder(t4.GetConstructor(new Type[] { typeof(string) }), new[] { "UserOnly" }));


                        //设置返回类型
                        //setPropMthdBldr.SetReturnType(apiMethodInfo.ReturnType);

                        //设置参数
                        var boundType = false;
                        //定义其他参数
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var p = parameters[i];
                            ParameterBuilder pb = setPropMthdBldr.DefineParameter(i + 1/*从1开始，0为返回值*/, p.Attributes, p.Name);
                            //处理参数，反之出现复杂类型的参数，抛出异常：InvalidOperationException: Action 'WeChat_OfficialAccountController.CardApi_GetOrderList (WeixinApiAssembly)' has more than one parameter that was specified or inferred as bound from request body. Only one parameter per action may be bound from body. Inspect the following parameters, and use 'FromQueryAttribute' to specify bound from query, 'FromRouteAttribute' to specify bound from route, and 'FromBodyAttribute' for parameters to be bound from body:
                            if (p.ParameterType.IsClass)
                            {
                                if (boundType == false)
                                {
                                    //第一个绑定，可以不处理
                                    boundType = true;
                                }
                                else
                                {
                                    //第二个开始使用标签
                                    var tFromQuery = typeof(FromQueryAttribute);
                                    pb.SetCustomAttribute(new CustomAttributeBuilder(tFromQuery.GetConstructor(new Type[0]), new object[0]));
                                }
                            }
                            try
                            {
                                //设置默认值
                                if (p.HasDefaultValue)
                                {
                                    pb.SetConstant(p.DefaultValue);
                                }
                            }
                            catch (Exception)
                            {
                                throw;
                            }

                        }

                        //执行具体方法
                        var il = setPropMthdBldr.GetILGenerator();

                        //FieldBuilder fb = tb.DefineField("id", typeof(System.String), FieldAttributes.Private);

                        var methodInfo = apiMethodInfo;

                        LocalBuilder local = null;
                        if (methodInfo.ReturnType != typeof(void))
                        {
                            //il.Emit(OpCodes.Ldstr, "The I.M implementation of C");
                            local = il.DeclareLocal(apiMethodInfo.ReturnType); // create a local variable
                                                                               //il.Emit(OpCodes.Ldarg_0);
                                                                               //动态创建字段   
                        }


                        //il.Emit(OpCodes.Ldarg_0); // this  //静态方法不需要使用this
                        //il.Emit(OpCodes.Ldarg_1); // the first one in arguments list
                        il.Emit(OpCodes.Nop); // the first one in arguments list
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var p = parameters[i];
                            //WriteLog($"\t\t Ldarg: {p.Name}\t isOptional:{p.IsOptional}\t defaultValue:{p.DefaultValue}");
                            il.Emit(OpCodes.Ldarg, i + 1); // the first one in arguments list
                        }

                        //WriteLog($"\t get static method: {methodInfo.Name}\t returnType:{methodInfo.ReturnType}");

                        il.Emit(OpCodes.Call, methodInfo);

                        ////if (apiMethodInfo.GetType() == apiMethodInfo.DeclaringType)//注意：此处使用不同的方法，会出现不同的异常
                        //if (typeof(Senparc.Weixin.MP.CommonAPIs.CommonApi) == methodInfo.DeclaringType)
                        //    il.Emit(OpCodes.Call, methodInfo);
                        //else
                        //    il.Emit(OpCodes.Callvirt, methodInfo);

                        if (methodInfo.ReturnType != typeof(void))
                        {
                            il.Emit(OpCodes.Stloc, local); // set local variable
                            il.Emit(OpCodes.Ldloc, local); // load local variable to stack 
                                                           //il.Emit(OpCodes.Pop);
                        }

                        il.Emit(OpCodes.Ret);

                        //生成文档
                        var docName = $"{methodInfo.DeclaringType.FullName}.{methodInfo.Name}(";//以(结尾确定匹配到完整的方法名
                        WriteLog($"\t search for docName:  {docName}\t\tSDK Method：{apiMethodInfo.ToString()}");

                        var docMember = docMembers.FirstOrDefault(z => z.HasAttributes && z.FirstAttribute.Value.Contains(docName));
                        if (docMember != null)
                        {
                            var attr = docMember.FirstAttribute;
                            var newAttrName = $"M:{tb.FullName}.{methodName}({attr.Value.ToString().Split('(')[1]}";
                            attr.SetValue(newAttrName);
                            //WriteLog($"\t change document name:  {attr.Value} -> {newAttrName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        //遇到错误
                        WriteLog($"==== Error ====\r\n \t{ex.ToString()}");
                    }
                }

                TypeInfo objectTypeInfo = tb.CreateTypeInfo();
                var myType = objectTypeInfo.AsType();

                WriteLog($"\t create type:  {myType.Namespace} - {myType.FullName}");

                //WeixinApiAssembly = myType.Assembly;//注意：此处会重复赋值相同的对象，不布偶股影响效率

                var newDocFile = $"App_Data/ApiDocXml/{assembleName}.xml";
                try
                {
                    document.Save(newDocFile);//保存
                    WriteLog($"new document file saved: {newDocFile}, assembly cost:{SystemTime.NowDiff(groupStartTime)}");
                }
                catch (Exception ex)
                {
                    WriteLog($"save document xml faild: {ex.Message}\r\n{ex.ToString()}");
                }

                WeixinApiAssemblyCollection[apiGroup.Key] = mb.Assembly;//储存程序集
            }

            var timeCost = SystemTime.NowDiff(dt1);

            WriteLog($"==== Finish BuildWebApi / Total Time: {timeCost.TotalMilliseconds:###,###} ms ====");

            #endregion
        }

        /// <summary>
        /// 获取 WeixinApiAssembly 程序集对象
        /// </summary>
        /// <returns></returns>
        public Assembly GetWeixinApiAssembly(PlatformType platformType)
        {
            if (!WeixinApiAssemblyCollection.ContainsKey(platformType))
            {
                BuildWebApi();
            }

            return WeixinApiAssemblyCollection[platformType];
        }
    }
}
