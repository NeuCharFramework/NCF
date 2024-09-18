using Senparc.Xncf.Tenant.Domain.DataBaseModel;

namespace Senparc.Xncf.Installer.Domain.Dto
{
    public class InstallResponseDto
    {
        public int StatCode { get; set; }
        public string SystemName { get; set; }
        public string AdminUserName { get; set; }
        public string AdminPassword { get; set; }
        public int Step { get; set; }
        public TenantInfoDto TenantInfoDto { get; set; }
        public List<XncfRegisterDto> NeedModelList { get; set; }
    }
}
