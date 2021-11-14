using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Core.Models
{
    [Serializable]
    public partial class FeedBack : EntityBase<int>
    {
        public int AccountId { get; set; }

        public string Content { get; set; }

        /// <summary>
        /// 用户
        /// </summary>
        /// <value></value>
        public Account Account { get; set; }
    }
}