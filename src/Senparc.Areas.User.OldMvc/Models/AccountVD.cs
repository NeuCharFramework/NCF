using Senparc.Ncf.Core.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Areas.User.Models.VD
{
    public class BaseAccountVD : BaseUserVD
    { }

    public class Account_IndexVD : BaseAccountVD
    { }

    public class Account_InfoVD : BaseAccountVD
    {
        public Account Account { get; set; }
    }


    public class Account_BuyVD : BaseAccountVD
    {
       
    }
    public class Account_PayLogVD : BaseAccountVD
    {
        public PagedList<AccountPayLog> PayLogList { get; internal set; }
    }

    public class BasicEdit
    {
        public string RealName { get; set; }

        [EmailAddress(ErrorMessage = "请输入正确邮箱地址")]
        public string Email { get; set; }
    }

    public class PasswordEdit
    {
        [Required(ErrorMessage = "请输入旧密码")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "请输入新密码")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "请输入确定密码")]
        [Compare("NewPassword", ErrorMessage = "两次输入密码不一致")]
        public string ConfirmNewPassword { get; set; }
    }
}
