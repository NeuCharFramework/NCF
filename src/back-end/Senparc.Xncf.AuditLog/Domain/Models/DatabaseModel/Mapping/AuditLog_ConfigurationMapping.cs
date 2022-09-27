using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Xncf.AuditLog.Models
{
    [XncfAutoConfigurationMapping]
    public class AuditLog_ConfigurationMapping : ConfigurationMappingWithIdBase<AuditLogInfo, string>
    {
        public override void Configure(EntityTypeBuilder<AuditLogInfo> builder)
        {
           
        }
    }
}
