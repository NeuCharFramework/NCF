using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Xncf.Accounts.Domain.Models
{
    public class AccountPayLogConfigurationMapping : ConfigurationMappingWithIdBase<AccountPayLog, int>
    {
        public override void Configure(EntityTypeBuilder<AccountPayLog> builder)
        {
            builder.Property(e => e.OrderNumber).HasColumnType("varchar(100)").IsRequired();
           
            builder.Property(e => e.UsedPoints).HasColumnType("decimal(18, 2)").IsRequired(false);
          
            builder.Property(e => e.AddIp).HasColumnType("varchar(50)").IsRequired(false);
            builder.Property(e => e.CompleteTime).IsRequired();
            builder.Property(e => e.Description).HasColumnType("varchar(250)").IsRequired();
            builder.Property(e => e.TradeNumber).HasColumnType("varchar(150)").IsRequired(false);
            builder.Property(e => e.PrepayId).HasColumnType("varchar(100)").IsRequired(false);

            string moneyType = null;
            var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
            if (currentDatabaseConfiguration.MultipleDatabaseType == MultipleDatabaseType.SqlServer)
            {
                moneyType = "money";
            }
            else
            {
                moneyType = "decimal(18, 2)";
            }

            builder.Property(e => e.Fee).HasColumnType(moneyType).IsRequired();
            builder.Property(e => e.Fee).HasColumnType(moneyType).IsRequired();
            builder.Property(e => e.GetPoints).HasColumnType(moneyType).IsRequired();

            builder.HasMany(z => z.PointsLogs).WithOne(z => z.AccountPayLog).HasForeignKey(z => z.AccountPayLogId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
