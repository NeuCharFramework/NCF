using AutoMapper;
using Senparc.Areas.Admin.OHS.PL;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.AutoMpperProfiles
{
    public class SenparcAreaAdminAutoMapperProfile: Profile
    {
        public SenparcAreaAdminAutoMapperProfile()
        {
            CreateMap<AdminUserInfo_CreateOrUpdateRequest, CreateOrUpdate_AdminUserInfoDto>();
        }
    }
}
