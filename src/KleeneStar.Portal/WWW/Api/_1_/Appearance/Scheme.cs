using KleeneStar.Core.WebRestApi;
using WebExpress.WebCore.WebAttribute;

namespace KleeneStar.Portal.WWW.Api._1_.Appearance
{
    /// <summary>
    /// The light or dark choice of the portal (<c>/portal/api/1/appearance/scheme</c>) - the endpoint the dark mode entry of the avatar menu stores into; the behaviour is the core's <see cref="RestApiAppearanceScheme"/>, so a choice made here is the
    /// one the operator side shows, and the other way round.
    /// </summary>
    [Title("kleenestar.core:profile.appearance.scheme.label")]
    public sealed class Scheme : RestApiAppearanceScheme
    {
    }
}
