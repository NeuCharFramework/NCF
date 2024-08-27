using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Areas.Admin.Domain.Models
{
    /// <summary>
    /// SenparcDbContextFactory 的公共配置
    /// </summary>
    public static class SenparcDbContextFactoryConfig
    {
        private static string _rootDirectoryPath = null;

        /// <summary>
        /// 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息
        /// </summary>
        public static string RootDirectoryPath
        {
            get
            {
                if (_rootDirectoryPath == null)
                {
                    var projectPath = Path.GetFullPath($"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}", AppContext.BaseDirectory);//项目根目录

                    var webPath = Path.GetFullPath($"..{Path.DirectorySeparatorChar}Senparc.Web",/*找到 Web目录，以获取统一的数据库连接字符串配置*/
                                                   projectPath);
                    if (Directory.Exists(webPath))
                    {
                        _rootDirectoryPath = webPath;//优先使用Web统一配置
                    }
                    else
                    {
                        _rootDirectoryPath = projectPath;
                    }
                }
                return _rootDirectoryPath;
            }
        }
    }
}
