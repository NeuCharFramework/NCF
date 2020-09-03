using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.FileServer.Models.DatabaseModel
{
    public class FileServerSenparcEntities : XncfDatabaseDbContext
    {
        public override IXncfDatabase XncfDatabaseRegister => new Register();
        public FileServerSenparcEntities(DbContextOptions<FileServerSenparcEntities> dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<SysKey> SysKeys { get; set; }
        public DbSet<FileRecord> FileRecords { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
