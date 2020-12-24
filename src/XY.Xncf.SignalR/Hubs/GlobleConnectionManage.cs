using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace XY.Xncf.SignalR.Hubs
{
    public class GlobleConnectionManage
    {
        public static ConcurrentDictionary<string, List<Client>> ClientList = new ConcurrentDictionary<string, List<Client>>();
    }
}
