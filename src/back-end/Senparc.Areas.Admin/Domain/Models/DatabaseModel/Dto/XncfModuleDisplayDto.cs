/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfModuleDisplayDto.cs
    文件功能描述：后台菜单多语言显示与兼容处理
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.12.28 按稳定模块标识解析内置菜单多语言显示，保留用户自定义菜单原文

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
namespace Senparc.Areas.Admin.Domain
{
    /// <summary>
    /// XNCF 模块显示 DTO
    /// </summary>
    public class XncfModuleDisplayDto : DtoBase
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Uid { get; private set; }
        public string MenuName { get; private set; }
        public string Version { get; private set; }
        public string Description { get; private set; }
        public string UpdateLog { get; private set; }
        public bool AllowRemove { get; private set; }
        public string MenuId { get; private set; }
        public string Icon { get; private set; }
        public XncfModules_State State { get; private set; }

        /// <summary>
        /// 是否有新版本
        /// </summary>
        public bool HasNewVersion { get; set; }

        /// <summary>
        /// 新版本号
        /// </summary>
        public string NewVersion { get; set; }
        public object Menus { get; set; }

        /// <summary>
        /// 模块Functions
        /// </summary>
        public object Functions { get; set; }

        private XncfModuleDisplayDto() { }

        /// <summary>
        /// Replaces persisted display text with the current registered module
        /// metadata without changing the database record.
        /// </summary>
        public void ApplyLocalizedMetadata(string menuName, string description)
        {
            MenuName = menuName;
            Description = description;
        }
 

        public XncfModuleDisplayDto(int id, string name, string uid, string menuName, string version, string description, string updateLog, bool allowRemove, string menuId, string icon, XncfModules_State state, bool hasNewVersion, string newVersion, object menus, object functions = null)
        {
            Id = id;
            Name = name;
            Uid = uid;
            MenuName = menuName;
            Version = version;
            Description = description;
            UpdateLog = updateLog;
            AllowRemove = allowRemove;
            MenuId = menuId;
            Icon = icon;
            State = state;
            HasNewVersion = hasNewVersion;
            NewVersion = newVersion;
            Menus = menus;
            Functions = functions;
        }

    }
}
