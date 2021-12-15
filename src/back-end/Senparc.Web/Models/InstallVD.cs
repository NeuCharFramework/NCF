namespace Senparc.Web.Models.VD
{
    public class Install_BaseVD : BaseVD { }

    public class Install_IndexVD : Install_BaseVD
    {
        public string AdminUserName { get; set; }
        public string AdminPassword { get; set; }
    }
}