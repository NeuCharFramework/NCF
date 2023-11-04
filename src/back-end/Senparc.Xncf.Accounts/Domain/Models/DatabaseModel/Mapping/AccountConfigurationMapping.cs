using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Xncf.Accounts.Domain.Models
{
    public class AccountConfigurationMapping : ConfigurationMappingWithIdBase<Account,int>
    {
        public override void Configure(EntityTypeBuilder<Account> builder)
        {
            //builder.HasKey(z => z.Id);
            //builder.Property(e => e.AddTime).HasColumnType("datetime").IsRequired();

            builder.Property(e => e.City).HasMaxLength(30);

            builder.Property(e => e.Country).HasMaxLength(30);
            builder.Property(z => z.Flag).HasDefaultValue(false);

            builder.Property(e => e.NickName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.Password).HasMaxLength(100);

            builder.Property(e => e.PasswordSalt)
                .HasMaxLength(100)
                .IsUnicode(false);

            builder.Property(e => e.Phone).HasMaxLength(20);

            builder.Property(e => e.PicUrl)
                .HasMaxLength(300)
                .IsUnicode(false);

            builder.Property(e => e.Province).HasMaxLength(20);

            builder.Property(e => e.RealName).HasMaxLength(100);

            builder.Property(e => e.ThisLoginIp)
                .HasColumnName("ThisLoginIP")
                .HasMaxLength(30)
                .IsUnicode(false);

            //builder.Property(e => e.ThisLoginTime).HasColumnType("datetime");


            builder.Property(e => e.UserName)
                .IsRequired()
                .HasMaxLength(50);
          

            builder.HasMany(z => z.PointsLogs).WithOne(z => z.Account).HasForeignKey(z => z.AccountId).OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(z => z.AccountPayLogs).WithOne(z => z.Account).HasForeignKey(z => z.AccountId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
