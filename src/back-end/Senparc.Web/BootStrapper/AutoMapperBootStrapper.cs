using AutoMapper;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Web
{
    public static class AutoMapperBootStrapper
    {
        public static void UseAutoMapper(this IApplicationBuilder app)
        {
            //Mapper.Initialize(y =>
            //{
            //    #region User

            //    //y.CreateMap<Account, AccountSame>().ForAllOtherMembers(z => z.ExplicitExpansion());
            //    //y.CreateMap<Account, AccountBusinessVD>().ForAllOtherMembers(z => z.ExplicitExpansion());

            //    #endregion
            //});
        }
    }
}
