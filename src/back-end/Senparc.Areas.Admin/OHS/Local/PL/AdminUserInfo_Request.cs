using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.PL
{
    public class AdminUserInfo_LoginRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(10)]
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
