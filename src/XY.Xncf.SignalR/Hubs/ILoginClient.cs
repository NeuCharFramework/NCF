using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace XY.Xncf.SignalR.Hubs
{
    /// <summary>
    /// 定义强类型中心
    /// </summary>
    public interface ILoginClient
    {
        /// <summary>
        /// 登出 限制仅允许一个终端在线
        /// </summary>
        /// <returns></returns>
        Task LoginOut();
    }
}
