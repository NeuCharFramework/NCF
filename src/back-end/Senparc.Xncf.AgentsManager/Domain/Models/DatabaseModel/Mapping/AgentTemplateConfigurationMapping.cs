using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping
{
    [XncfAutoConfigurationMapping]
    public class AgentTemplateConfigurationMapping : ConfigurationMappingWithIdBase<AgentTemplate, int>
    {
        public override void Configure(EntityTypeBuilder<AgentTemplate> builder)
        {
            base.Configure(builder);
        }
    }
}
