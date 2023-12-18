using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;

namespace SenparcConf.Xncf.WechatBot.Models
{
    public class WechatBotSenparcEntities : XncfDatabaseDbContext
    {
        public WechatBotSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Color> Colors { get; set; }

        /// <summary>
        /// 用于记录对话消息，其中包含：收到的消息、返回的消息、查询消息使用时间、AccountId
        /// </summary>
        public DbSet<Message> DialogMessages { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
