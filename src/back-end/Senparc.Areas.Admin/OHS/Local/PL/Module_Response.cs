using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{
    public class Module_StatResponse
    {
        /// <summary>
        /// 已安装模块数量
        /// </summary>
        public int InstalledXncfCount { get; set; }
        /// <summary>
        /// 待更新模块数量
        /// </summary>
        public int UpdateVersionXncfCount { get; set; }
        /// <summary>
        /// 新模块数量
        /// </summary>
        public int NewXncfCount { get; set; }
        /// <summary>
        /// 异常模块数量
        /// </summary>
        public int MissingXncfCount { get; set; }
    }

    public class Module_GetItemResponse
    {
        /// <summary>
        /// 必须刷新页面，MustUpdate 为 true 时，必定有异常信息
        /// </summary>
        public bool MustUpdate { get; set; }
        /// <summary>
        /// 模块信息
        /// </summary>
        public XncfModuleDto XncfModule { get; set; }
        /// <summary>
        /// XNCF 模块注册信息
        /// </summary>
        public Response_XncfRegister XncfRegister { get; set; }

        public List<Response_FunctionParameterInfoCollection> FunctionParameterInfoCollection { get; set; }

        public class Response_XncfRegister
        {
            /// <summary>
            /// 主页 URL
            /// </summary>
            public string AreaHomeUrl { get; set; }
            /// <summary>
            /// 菜单显示名称
            /// </summary>
            public string MenuName { get; set; }
            /// <summary>
            /// 图标
            /// </summary>
            public string Icon { get; set; }
            /// <summary>
            /// 版本
            /// </summary>
            public string Version { get; set; }
            /// <summary>
            /// 唯一编号
            /// </summary>
            public string Uid { get; set; }

            /// <summary>
            /// 子菜单项目列表
            /// </summary>
            public List<Ncf.Core.Areas.AreaPageMenuItem> AreaPageMenuItems { get; set; }

            /// <summary>
            /// 接口
            /// </summary>
            public List<string> Interfaces { get; set; }

            /// <summary>
            /// “执行方法”数量统计
            /// </summary>
            public int FunctionCount { get; set; }

            /// <summary>
            /// 线程信息
            /// </summary>
            public IEnumerable<Response_XncfRegister_RegisteredThreadInfo> RegisteredThreadInfo { get; set; }


            public class Response_XncfRegister_RegisteredThreadInfo
            {
                /// <summary>
                /// 线程信息
                /// </summary>
                public RegisteredThreadInfo_Key Key { get; set; }
                /// <summary>
                /// 线程状态详情
                /// </summary>
                public RegisteredThreadInfo_Value Value { get; set; }

                public class RegisteredThreadInfo_Key
                {
                    /// <summary>
                    /// 线程名称
                    /// </summary>
                    public string Name { get; set; }
                    /// <summary>
                    /// 线程故事
                    /// </summary>
                    public string StoryHtml { get; set; }
                }
                public class RegisteredThreadInfo_Value
                {
                    public bool IsAlive { get; set; }
                    public bool? IsBackground { get; set; }
                    public ThreadState? ThreadState { get; set; }
                    public string ThreadStateStr { get; set; }

                }
            }
        }

        public class Response_FunctionParameterInfoCollection
        {
            public Response_FunctionParameterInfoCollection_Key Key { get; set; }
            public List<FunctionParameterInfo> Value { get; set; }

        }

        public class Response_FunctionParameterInfoCollection_Key
        {
            public string Name { get; set; }
            public string Description { get; set; }

            public Response_FunctionParameterInfoCollection_Key(string name, string description)
            {
                Name = name;
                Description = description;
            }

        }
    }

    public class Module_RunFunctionResponse
    {
        public string Msg { get; set; }
        public string Log { get; set; }
        public string Exception { get; set; }
        public string TempId { get; set; }
    }
}
