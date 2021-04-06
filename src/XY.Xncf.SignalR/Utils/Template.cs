using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace XY.Xncf.SignalR
{
    public class Template
    {
        private static string _signalRClientTemplate { get; set; }
        /// <summary>
        /// 客户端SignalR代码
        /// </summary>
        public static string SignalRClientTemplate
        {
            get
            {
                if (_signalRClientTemplate == null)
                    _signalRClientTemplate = GetSignalRClientTemplate("SignalR");
                return _signalRClientTemplate;
            }
        }

        private static string _onlineUserTemplate { get; set; }
        public static string OnlineUserTemplate
        {
            get
            {
                if (_onlineUserTemplate == null)
                    _onlineUserTemplate = GetSignalRClientTemplate("OnlineUser");
                return _onlineUserTemplate;
            }
        }
        public static string GetSignalRClientTemplate(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string[] resNames = assembly.GetManifestResourceNames();  //列出所有资源名称

            //获取指定的资源
            using (Stream stream = assembly.GetManifestResourceStream($"XY.Xncf.SignalR.Pages.Shared.{resourceName}.cshtml"))
            {
                if (stream != null)  //没有找到，GetManifestResourceStream会返回null
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            return "";
        }
    }
}
