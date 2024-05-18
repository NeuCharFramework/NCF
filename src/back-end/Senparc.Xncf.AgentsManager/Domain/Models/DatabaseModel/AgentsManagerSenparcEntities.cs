using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;

namespace Senparc.Xncf.AgentsManager.Models
{
    public class AgentsManagerSenparcEntities : XncfDatabaseDbContext
    {
        public AgentsManagerSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<AgentTemplate> AgentTemplates { get; set; }

        public DbSet<ChatGroup> ChatGroups { get; set; }

        public DbSet<ChatGroupMember> ChatGroupMembers { get; set; }

        public DbSet<ChatGroupHistory> ChatGroupHistories { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
