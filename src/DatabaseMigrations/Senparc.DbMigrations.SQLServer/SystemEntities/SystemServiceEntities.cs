using Microsoft.EntityFrameworkCore;
using Senparc.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.DbMigrations.SQLServer
{
    /// <summary>
    /// 当前 Entities 只为对接 Service 的 Register 而存在（必须继承自 XncfDatabaseDbContext），没有特别的意义。
    /// TODO：让 SenparcEntities 继承 XncfDatabaseDbContext，并提供 Senparc.Core 的 Register。
    /// </summary>
    public class SystemServiceEntities : XncfDatabaseDbContext
    {
        public override IXncfDatabase XncfDatabaseRegister => new Senparc.Service.Register();
        public SystemServiceEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }
    }
}
