using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Core.Models
{
    [XncfAutoConfigurationMapping]
    public class FeedbackConfigurationMapping : ConfigurationMappingWithIdBase<FeedBack, int>
    {
        public override void Configure(EntityTypeBuilder<FeedBack> builder)
        {
            //builder.HasQueryFilter(z => !z.Flag);
        }
    }
}