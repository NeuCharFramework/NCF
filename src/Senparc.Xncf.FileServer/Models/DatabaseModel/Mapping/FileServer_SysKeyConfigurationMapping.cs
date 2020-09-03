using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Xncf.FileServer.Models
{
    //[XncfAutoConfigurationMapping]
    public class FileServer_SysKeyConfigurationMapping : ConfigurationMappingWithIdBase<SysKey, int>
    {
        public override void Configure(EntityTypeBuilder<SysKey> builder)
        {
            base.Configure(builder);

            builder.HasKey(k => k.Id);
            builder.Property(entity => entity.AppId).HasMaxLength(32).IsRequired();
            builder.Property(entity => entity.AppKey).HasMaxLength(64).IsRequired();
            builder.Property(entity => entity.Name).HasMaxLength(50).IsRequired();
            //builder.Property(entity => entity.CreateTime).HasColumnType("DATETIME").HasDefaultValueSql("GETDATE()").IsRequired();
            //builder.Property(entity => entity.ModifiedTime).HasColumnType("DATETIME").HasDefaultValueSql("GETDATE()");
            //builder.Property(entity => entity.Remark).HasMaxLength(300);

            builder.HasMany(m => m.FileRecords).WithOne(o => o.SysKey).HasForeignKey(k => k.SysKeyId);
        }
    }
}
