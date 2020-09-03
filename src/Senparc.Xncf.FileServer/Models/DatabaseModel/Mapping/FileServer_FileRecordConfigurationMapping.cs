using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Xncf.FileServer.Models
{
    //[XncfAutoConfigurationMapping]
    public class FileServer_FileRecordConfigurationMapping : ConfigurationMappingWithIdBase<FileRecord, int>
    {
        public override void Configure(EntityTypeBuilder<FileRecord> builder)
        {
            base.Configure(builder);

            builder.HasKey(k => k.Id);
            builder.Property(entity => entity.FileContentType).HasMaxLength(150).IsRequired();
            builder.Property(entity => entity.FileName).HasMaxLength(150).IsRequired();
            builder.Property(entity => entity.PrefixPath).HasMaxLength(50);
            builder.Property(entity => entity.FilePath).HasMaxLength(200).IsRequired();
            builder.Property(entity => entity.FileHash).HasMaxLength(64).IsRequired();
            //builder.Property(entity => entity.CreateTime).HasColumnType("DATETIME").HasDefaultValueSql("GETDATE()").IsRequired();
            //builder.Property(entity => entity.ModifiedTime).HasColumnType("DATETIME").HasDefaultValueSql("GETDATE()");
            //builder.Property(entity => entity.Remark).HasMaxLength(300);
        }
    }
}
