using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace SenparcLive.Xncf.MyApp.Models.DatabaseModel
{
    [MultipleMigrationDbContext(MultipleDatabaseType.SQLite, typeof(Register))]
    public class MyAppSenparcEntities : XncfDatabaseDbContext
    {
        

        public MyAppSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Color> Colors { get; set; }
        public DbSet<Counter> Counter { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
