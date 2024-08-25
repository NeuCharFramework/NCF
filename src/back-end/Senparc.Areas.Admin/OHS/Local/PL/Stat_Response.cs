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
        public int LogCount { get; set; }
    }
}
