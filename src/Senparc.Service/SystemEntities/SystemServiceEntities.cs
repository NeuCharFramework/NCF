using Microsoft.EntityFrameworkCore;
using Senparc.Core.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Service
{
    /// <summary>
    /// 当前 Entities 只为帮助 SenparcEntities 生成 Migration 信息而存在，没有特别的操作意义。
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.SQLite, typeof(Senparc.Service.Register))]
    public class SystemServiceEntities : SenparcEntities
    {
        public SystemServiceEntities(DbContextOptions/*<SystemServiceEntities>*/ dbContextOptions) : base(dbContextOptions)
        {
        }
    }
}
