using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{

    public class Module_StatResponse
    {
        public int InstalledXncfCount { get; set; }
        public int UpdateVersionXncfCount { get; set; }
        public int NewXncfCount { get; set; }
        public int MissingXncfCount { get; set; }
    }
}
