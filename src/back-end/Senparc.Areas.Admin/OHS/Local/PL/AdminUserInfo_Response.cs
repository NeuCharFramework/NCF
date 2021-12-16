using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{
    public class AdminUserInfo_GetListResponse
    {
        public IList<AdminUserInfoDto> List { get; set; }
        public int TotalCount { get; set; }
    }

    public class AdminUserInfo_CreateResponse
    {
        public int AdminUserInfoId { get; set; }
    }
}
