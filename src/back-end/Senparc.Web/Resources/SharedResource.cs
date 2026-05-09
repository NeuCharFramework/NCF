namespace Senparc.Web
{
    /// <summary>
    /// Marker class for shared localization resources.
    /// Resource files are stored in Resources/SharedResource.{culture}.resx
    /// 
    /// Usage in views: @inject IStringLocalizer&lt;SharedResource&gt; SR
    /// Usage in code:  IStringLocalizer&lt;SharedResource&gt; localizer (via DI)
    /// 
    /// Supported cultures: zh-CN (default), en, ja, fr, es, ru
    /// </summary>
    public class SharedResource
    {
    }
}
