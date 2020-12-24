using AutoMapper.Configuration;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace XY.Xncf.SignalR.Hubs
{
    public class LoginHub : Hub<ILoginClient>
    {
        public LoginHub(/*可在构造函数中注入需要的服务*/)
        {
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var clientId = httpContext.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(clientId))
            {
                int? userId = null;
                string userName = null;
                if (httpContext.Items.TryGetValue("userId", out object _userId))
                {
                    userId = (int)_userId;
                    userName = httpContext.Items["userName"]?.ToString();
                }
                var clientList = GlobleConnectionManage.ClientList.GetOrAdd(clientId.ToString(), (key) =>
                {
                    return new List<Client>();
                });
                clientList.Add(new Client() { UserId = userId, UserName = userName, ConnectionId = Context.ConnectionId });

                //var clientIdList = GlobleConnectionManage.ClientList.Where(i => i.Value.Count(c => c.UserId == userId) > 0).Select(i => i.Key).ToList();
                //await Clients.Groups(clientIdList).LoginOut();//所有连接客户端，执行退出
                await Groups.AddToGroupAsync(Context.ConnectionId, clientId.ToString());//当前客户端添加到分组
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            var clientId = httpContext.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(clientId))
            {
                var clientList = GlobleConnectionManage.ClientList.GetOrAdd(clientId, (key) =>
                {
                    return new List<Client>();
                });
                var client = clientList.Find(f => f.ConnectionId == Context.ConnectionId);
                if (client != null) clientList.Remove(client);
                if (clientList.Count == 0)
                    GlobleConnectionManage.ClientList.TryRemove(clientId, out List<Client> outClientList);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, clientId);//当前客户端添加到分组

                //根据ConnectionId直接处理
                //var client = GlobleConnectionManage.ClientList.FirstOrDefault(f => f.Value.Any(a => a.ConnectionId == Context.ConnectionId));
                //client.Value.Remove(client.Value.Find(w => w.ConnectionId == Context.ConnectionId));
                //if (client.Value.Count == 0)
                //    GlobleConnectionManage.ClientList.TryRemove(client.Key, out List<Client> outClientList);
                //await Groups.RemoveFromGroupAsync(Context.ConnectionId, client.Key);//当前客户端添加到分组
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
