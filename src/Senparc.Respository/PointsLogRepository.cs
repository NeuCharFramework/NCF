using Senparc.Core.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Repository
{
    public interface IPointsLogRepository : IClientRepositoryBase<PointsLog>
    {
    }

    public class PointsLogRepository : ClientRepositoryBase<PointsLog>, IPointsLogRepository
    {
        public PointsLogRepository(INcfClientDbData NcfClientDbData) : base(NcfClientDbData)
        {

        }
    }
}

