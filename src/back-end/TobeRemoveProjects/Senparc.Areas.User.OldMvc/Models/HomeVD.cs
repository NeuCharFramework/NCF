using System;

namespace Senparc.Areas.User.Models.VD
{
    public class BaseHomeVD : BaseUserVD
    {
    }

    public class Home_IndexVD : BaseHomeVD
    {
    }
    public class Home_RecoverVD : BaseHomeVD
    {
    }
    public class Home_LoginVD : BaseHomeVD
    {
        public string ReturnUrl { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        public bool ShowCheckCode { get; set; }
    }
    public class Home_RegisterVD : BaseHomeVD
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
}
