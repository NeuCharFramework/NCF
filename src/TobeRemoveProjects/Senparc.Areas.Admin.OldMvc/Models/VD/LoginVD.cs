using System.ComponentModel.DataAnnotations;
using Senparc.Ncf.Core.Models.VD;

namespace Senparc.Areas.Admin.Models.VD
{
    public class Login_BaseVD : BaseVD
    {

    }


    public class Login_IndexVD : Login_BaseVD
    {
        [Required(ErrorMessage = "请输入用户名")]
        public string Name { get; set; }

        [Required(ErrorMessage = "请输入密码")]
        public string Password { get; set; }

        public string ReturnUrl { get; set; }
        public bool IsLogined { get; set; }
    }
}
