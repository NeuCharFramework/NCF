using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class AgentsTemplateService : ServiceBase<AgentTemplate>
    {
        public AgentsTemplateService(IRepositoryBase<AgentTemplate> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        public async Task<AgentTemplate> GetAgentTemplateAsync(int id)
        {
            return await base.GetObjectAsync(x => x.Id == id);
        }

        /// <summary>
        /// 使用 AgentTemplateDto 进行更新
        /// </summary>
        /// <param name="id"></param>
        /// <param name="agentTemplateDto"></param>
        /// <returns></returns>
        public async Task UpdateAgentTemplateAsync(int id, AgentTemplateDto agentTemplateDto)
        {
            AgentTemplate agentTemplate = null;

            if (id > 0)
            {
                agentTemplate = await GetAgentTemplateAsync(id);
            }

            if (agentTemplate == null)
            {
                agentTemplate = base.Mapper.Map<AgentTemplate>(agentTemplateDto);
            }

            agentTemplate.EnableAgent();

            await base.SaveObjectAsync(agentTemplate);
        }

    }
}
