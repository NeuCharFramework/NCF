using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Areas.Admin;

namespace Senparc.Xncf.MaQueKeTang.Models
{
    public class MaQueKeTangSenparcEntities : XncfDatabaseDbContext
    {
        public MaQueKeTangSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Color> Colors { get; set; }
        public DbSet<CourseCategory> CourseCategorys { get; set; }
        public DbSet<CourseChapter> CourseChapters { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseAttach> CourseAttachs { get; set; }
        public DbSet<CourseComment> CourseComments { get; set; }
        public DbSet<CourseUserRecord> CourseUserRecords { get; set; }
        public DbSet<CourseUser> CourseUsers { get; set; }
        public DbSet<CourseUserLike> CourseUserLikes { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<VideoUser> VideoUsers { get; set; }
        public DbSet<VideoUserComment> VideoUserComments { get; set; }
        public DbSet<VideoUserWatchRecord> VideoUserWatchRecords { get; set; }
        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// </summary>
    public class SenparcDbContextFactoryHeler
    {
      
    }
}
