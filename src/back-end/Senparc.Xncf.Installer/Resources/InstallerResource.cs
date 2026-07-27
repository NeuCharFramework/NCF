/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：InstallerResource.cs
    文件功能描述：安装模块本地化资源访问类
    
    
    创建标识：Senparc - 20260403
    
    修改标识：Senparc - 20260724
    修改描述：v0.4.0 增加默认模块选择与安装确认并完善多语言界面

----------------------------------------------------------------*/
namespace Senparc.Xncf.Installer
{
    /// <summary>
    /// Marker class for Installer module localization resources.
    /// Resource files are stored in Resources/InstallerResource.{culture}.resx
    ///
    /// Usage in Razor views: @inject IStringLocalizer&lt;InstallerResource&gt; IR
    /// Usage in code:        IStringLocalizer&lt;InstallerResource&gt; localizer (via DI)
    ///
    /// Supported cultures: zh-CN (default), en, ja, fr, es, ru
    /// To add a new language: copy InstallerResource.en.resx, rename to InstallerResource.{culture}.resx,
    /// translate the values, and add the culture code to NcfLocalizationOptions.SupportedCultures.
    /// </summary>
    public class InstallerResource
    {
    }
}
