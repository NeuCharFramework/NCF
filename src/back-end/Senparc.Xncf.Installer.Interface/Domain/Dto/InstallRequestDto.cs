namespace Senparc.Xncf.Installer.Domain.Dto
{
    public class InstallRequestDto
    {
        public string SystemName { get; set; }
        public string AdminUserName { get; set; }
        public string DbConnectionString { get; set; }
        public List<XncfRegister> IXncfRegisterModelList { get; set; }
    }
    public class XncfRegister
    {
        public bool IgnoreInstall { get; set; }
        public string Name { get; set; }
        public string Uid { get; set; }
        public string Version { get; set; }
        public string MenuName { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
    }
}
