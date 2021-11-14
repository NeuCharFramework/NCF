using Senparc.Core.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Repository
{
    public interface ISystemConfigRepository : IClientRepositoryBase<SystemConfig>
    {
    }

    public class SystemConfigRepository : ClientRepositoryBase<SystemConfig>, ISystemConfigRepository
    {
        public SystemConfigRepository(INcfClientDbData NcfClientDbData) : base(NcfClientDbData)
        {

        }
    }
}

