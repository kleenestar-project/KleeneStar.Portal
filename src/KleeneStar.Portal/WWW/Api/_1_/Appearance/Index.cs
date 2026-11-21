using KleeneStar.Core.WebRestApi;
using WebExpress.WebCore.WebAttribute;

namespace KleeneStar.Portal.WWW.Api._1_.Appearance
{
    /// <summary>
    /// The adjustments form of the portal profile (<c>/portal/api/1/appearance</c>); the behaviour is the core's <see cref="RestApiAppearance"/>, so a choice made here is the
    /// one the operator side shows, and the other way round.
    /// </summary>
    [Cache]
    public sealed class Index : RestApiAppearance
    {
    }
}
