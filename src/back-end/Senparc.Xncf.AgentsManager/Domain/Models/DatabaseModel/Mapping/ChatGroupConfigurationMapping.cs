using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping
{
    [XncfAutoConfigurationMapping]
    public class ChatGroupConfigurationMapping : ConfigurationMappingWithIdBase<ChatGroup, int>
    {
        public override void Configure(EntityTypeBuilder<ChatGroup> builder)
        {
            //throw new System.Exception("运行到这里了");
            base.Configure(builder);

            builder.HasOne(z => z.AdminAgentTemplate)
                   .WithMany(z => z.AdminChatGroups)
                   .HasForeignKey(z => z.AdminAgentTemplateId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(z => z.EnterAgentTemplate)
                   .WithMany(z => z.EnterAgentChatGroups)
                   .HasForeignKey(z => z.EnterAgentTemplateId)
                   .OnDelete(DeleteBehavior.NoAction);

        }

    }
}
