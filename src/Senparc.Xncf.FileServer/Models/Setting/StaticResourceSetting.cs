using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.FileServer
{
    /// <summary>
    /// 静态资源相关配置
    /// </summary>
    public class StaticResourceSetting
    {
        /// <summary>
        /// 上传根目录，可用软链接分离
        /// </summary>
        public string RootDir { get; set; }

        /// <summary>
        /// 请求根目录
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        /// 上传文件大小，单位：MB
        /// </summary>
        public int MaxSize { get; set; }

        /// <summary>
        /// 允许上传的文件
        /// </summary>
        public string[] AllowFile { get; set; }
    }
}
