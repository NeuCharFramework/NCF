using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class ChatGroupService : ServiceBase<ChatGroup>
    {
        public ChatGroupService(IRepositoryBase<ChatGroup> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        public async Task<string> RunGroup(int id)
        {
            var chatGroup = await base.GetObjectAsync(x => x.Id == id);



            return chatGroup.Name;
        }
    }
}
