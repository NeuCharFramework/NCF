/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfRegisterDto.cs
    文件功能描述：安装模块注册信息传输模型
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260724
    修改描述：v0.1.1 扩展安装模块元数据以支持默认选择与导航兼容

----------------------------------------------------------------*/
namespace Senparc.Xncf.Installer.Domain.Dto
{
    /* 模块信息数据传输对象*/
    public class XncfRegisterDto
    {
        public bool IgnoreInstall { get; set; }
        public bool SelectedByDefault { get; set; }
        public string Name { get; set; }
        public string Uid { get; set; }
        public string Version { get; set; }
        public string MenuName { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
    }
}
