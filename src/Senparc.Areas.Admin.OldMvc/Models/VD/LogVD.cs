using System;
using System.Collections.Generic;
using Senparc.Ncf.Core.Models;

namespace Senparc.Areas.Admin.Models.VD
{
    public class Log_BaseVD : BaseAdminVD
    {

    }

    public class Log_WebLogListVD : Log_BaseVD
    {
        public PagedList<string> DateList { get; set; }
    }

    public class Log_WebLogDateVD : Log_BaseVD
    {
        public int TotalPageCount { get; set; }
        public int PageIndex { get; set; }
        public DateTime CurrentDate { get; set; }
        public List<WebLog> LogList { get; set; }
    }
}
