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
