/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminResource.cs
    文件功能描述：后台管理本地化资源访问类
    
    
    创建标识：Senparc - 20260403
    
    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强后台模块批量更新并完善多语言管理界面

----------------------------------------------------------------*/
namespace Senparc.Areas.Admin
{
    /// <summary>
    /// Marker class for Admin module localization resources.
    /// Resource files are stored in Resources/AdminResource.{culture}.resx
    ///
    /// Usage in Razor views: @inject IStringLocalizer&lt;AdminResource&gt; AR
    /// Usage in code:        IStringLocalizer&lt;AdminResource&gt; localizer (via DI)
    ///
    /// Supported cultures: zh-CN (default), en, ja, fr, es, ru
    /// To add a new language: copy AdminResource.en.resx, rename to AdminResource.{culture}.resx,
    /// translate the values, and add the culture code to NcfLocalizationOptions.SupportedCultures.
    /// </summary>
    public class AdminResource
    {
    }
}
