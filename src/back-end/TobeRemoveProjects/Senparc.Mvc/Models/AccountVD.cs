using Senparc.Core.Models;
using Senparc.Core.Models.VD;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;
using System;

namespace Senparc.Mvc.Models.VD
{
    public class Account_BaseVD : BaseVD, IBaseVD
    {

    }

    public class Account_IndexVD : Account_BaseVD
    {
        public PagedList<Account> AccountList { get; set; }
    }
    public class Account_LoginVD : Account_BaseVD
    {
        public string ReturnUrl { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        public bool ShowCheckCode { get; set; }
    }
    public class Account_RegisterVD : Account_BaseVD
    {
        public new string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string RePassword { get; set; }
        public string Phone { get; set; }

        public string PhoneCheck { get; set; }
        public string QrCodeCheck { get; set; }

        public int LastSeconds { get; set; }
        public string SmsToken { get; set; }
        public Guid RegGuid { get; set; }
        public string QrUrl { get; set; }
    }
    public class Account_ResendLiveCode : Account_BaseVD
    {
        public Account Account { get; set; }
    }
}
