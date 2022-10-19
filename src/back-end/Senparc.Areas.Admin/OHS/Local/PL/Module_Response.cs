using System;
using System.Collections.Generic;
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
}
