using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SenparcLive.Xncf.MyApp.Models.DatabaseModel
{
    /// <summary>
    /// Color 实体类
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Counter))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class Counter : EntityBase<int>
    {
        public int ViewCount { get; private set; }

        private Counter() { }

        public Counter(int viewCount)
        {
            ViewCount = viewCount;
        }

        public int AddView()
        {
            return ++ViewCount;
        }
    }
}
