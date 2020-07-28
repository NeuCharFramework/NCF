using Senparc.Core.Models;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;

namespace Senparc.Areas.Admin.Models.VD
{
    public class Feedback_BaseVD : BaseAdminVD
    {

    }

    public class Feedback_IndexVD : Feedback_BaseVD
    {
        public string Kw { get; set; }
        public PagedList<FeedBack> FeedbackList { get; set; }
    }
}
