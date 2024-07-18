using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.DynamicData.Models.DatabaseModel;

namespace Senparc.Xncf.DynamicData.Models
{
    public class DynamicDataSenparcEntities : XncfDatabaseDbContext
    {
        public DynamicDataSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Color> Colors { get; set; }

        public DbSet<TableMetadata> TableMetadata { get; set; }
        public DbSet<ColumnMetadata> ColumnMetadata { get; set; }
        public DbSet<TableData> TableData { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
