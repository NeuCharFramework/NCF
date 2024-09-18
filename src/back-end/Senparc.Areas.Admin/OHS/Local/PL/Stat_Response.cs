using Senparc.Areas.Admin.SenparcTraceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{
    public class Stat_GetLogsResponse
    {
        public List<Stat_GetLogsResponse_Item> Logs { get; set; } = new List<Stat_GetLogsResponse_Item>();
    }

    public class Stat_GetLogsResponse_Item
    {
        public string Date { get; set; }
        public int TotalLogCount { get; set; }
        public int NormalLogCount { get; set; }
        public int ExceptionLogCount { get; set; }
    }

    public class Stat_GetTodayLogResponse
    {
        public string Date { get; set; }
        public List<Stat_GetTodayLogResponse_Item> Items { get; set; } = new List<Stat_GetTodayLogResponse_Item>();
    }

    public class Stat_GetTodayLogResponse_Item
    {
        public string SenparcTraceType { get; set; }
        public int Count { get; set; }
    }


}
