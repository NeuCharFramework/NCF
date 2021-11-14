using Senparc.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Repository
{
    public interface IFeedBackRepository : IClientRepositoryBase<FeedBack>
    {
    }

    public class FeedBackRepository : ClientRepositoryBase<FeedBack>, IFeedBackRepository
    {
        public FeedBackRepository(INcfClientDbData NcfClientDbData) : base(NcfClientDbData)
        {

        }
    }
}