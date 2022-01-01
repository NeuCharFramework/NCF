using AutoMapper;
using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.Areas.Admin.OHS.PL;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
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
            CreateMap<SysRole_CreateOrUpdateRequest, SysRoleDto>();
            CreateMap<SysMenu_CreateOrUpdateRequest, SysMenuDto>();
            CreateMap<SysRole, Domain.Dto.SysRoleListDto>();
            CreateMap<SysRole, Domain.Dto.SysRoleDetailDto>();
            CreateMap<Domain.Dto.PermissionRequestDto, Ncf.Core.Models.DataBaseModel.SysPermissionDto>();
        }
    }
}
