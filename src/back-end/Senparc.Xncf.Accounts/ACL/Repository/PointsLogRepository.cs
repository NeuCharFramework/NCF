using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Core.Models;

namespace Senparc.Service.ACL
{
    public interface IPointsLogRepository : IClientRepositoryBase<PointsLog>
    {
    }

    public class PointsLogRepository : ClientRepositoryBase<PointsLog>, IPointsLogRepository
    {
        public PointsLogRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {

        }
    }
}

