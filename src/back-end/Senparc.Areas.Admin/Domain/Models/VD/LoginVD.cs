namespace Senparc.Areas.Admin.Domain.Models.VD
{
    public class Login_BaseVD : BaseVD
    {

    }


    public class Login_IndexVD : Login_BaseVD
    {
        new public string UserName { get; set; }
        public string Password { get; set; }
        public string CheckCode { get; set; }
        public bool ShowCheckCode { get; set; }
        public string ReturnUrl { get; set; }
        public bool AuthorityNotReach { get; set; }
        public bool IsLogined { get; set; }
    }

    public class Login_QyLoginVD : Login_BaseVD
    {
        public string UserUserName { get; set; }
        public string Password { get; set; }
        public string Code { get; set; }
    }
}
