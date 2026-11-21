using KleeneStar.Core.WebRestApi;
using WebExpress.WebCore.WebAttribute;

namespace KleeneStar.Portal.WWW.Api._1_.Appearance
{
    /// <summary>
    /// The theme selector of the portal (<c>/portal/api/1/appearance/theme</c>); the behaviour is the core's <see cref="RestApiAppearanceTheme"/>, so a choice made here is the
    /// one the operator side shows, and the other way round.
    /// </summary>
    [Title("kleenestar.core:profile.appearance.theme.label")]
    public sealed class Theme : RestApiAppearanceTheme
    {
    }
}
