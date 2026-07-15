using Senparc.Core.Models.VD;
using Senparc.Ncf.Core.Models.VD;

namespace Senparc.Mvc.Models.VD
{
    public class Home_IndexVD : Home_BaseVD
    {
       
    }

    public class Home_AboutVD : Home_BaseVD
    {
        public string Version { get; set; }
    }

    public class Home_RegAgreementVD : Home_BaseVD
    {

    }
    public class Home_DetailVD : BaseVD
    {
    }

}