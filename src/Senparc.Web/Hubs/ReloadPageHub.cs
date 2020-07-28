using Microsoft.AspNetCore.SignalR;

namespace Senparc.Web.Hubs
{
    /// <summary>
    /// 刷新页面
    /// </summary>
    public class ReloadPageHub : Hub
    {
        public const string Route = "/reloadHub";
    }
}