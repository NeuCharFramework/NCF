using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using System;

namespace Senparc.Areas.User.Models.VD
{

    public class FeedBack_BaseVD : BaseUserVD
    {
    }

    public class FeedBack_IndexVD : FeedBack_BaseVD
    {
        public PagedList<FeedBack> ModelList { get; set; }
    }

    public class FeedBack_EditVD : FeedBack_BaseVD
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Content { get; set; }

        public DateTime AddTime { get; set; }

        /// <summary>
        /// 用户
        /// </summary>
        /// <value></value>
        public Account Account { get; set; }


        public bool IsEdit { get; set; }
    }
}