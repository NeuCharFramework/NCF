using Senparc.Xncf.Swagger.Utils;

namespace Senparc.Xncf.Swagger.Models
{
    public class CustomSwaggerAuth
    {
        public CustomSwaggerAuth() { }
        public CustomSwaggerAuth(string userName, string userPwd)
        {
            UserName = userName;
            UserPwd = userPwd;
        }
        public string UserName { get; set; }
        public string UserPwd { get; set; }
        public string AuthStr
        {
            get
            {
                return SecurityHelper.HMACSHA256(UserName + UserPwd);
            }
        }
    }
}
