using WebExpress.WebApp.WebTheme;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebTheme;

namespace KleeneStar.Portal.WebTheme
{
    /// <summary>
    /// The default theme of the portal: the framework's own colours, light or dark.
    /// </summary>
    /// <remarks>
    /// A theme is registered per application, so the core's <c>LightTheme</c> does not reach
    /// the portal. Without a default of its own the portal would take the first theme any plugin
    /// registered for it - with <c>KleeneStar.Theme</c> installed, a season. It ships no
    /// stylesheet, so the framework palette applies; a reader who picked the core's default
    /// stores no theme and gets this one here.
    /// </remarks>
    [Name("kleenestar.portal:theme.light.name")]
    [Description("kleenestar.portal:theme.light.description")]
    [ThemeMode(ThemeMode.Light)]
    public sealed class PortalLightTheme : IThemeWebApp
    {
    }
}
