using WebExpress.WebCore.WebScope;

namespace KleeneStar.Portal.WebScope
{
    /// <summary>
    /// Marker interface that groups all customer-portal pages and their fragments. Used by
    /// the navigation chrome and topbar fragments so the portal shell shows portal-specific
    /// links (Start, Meine Vorgänge, Organisation) instead of the operator-side workspace
    /// chrome.
    /// </summary>
    /// <remarks>
    /// The portal deliberately does <strong>not</strong> share <c>IScopeGeneral</c> with the
    /// operator WebApp — making the audience switch obvious to anyone using both surfaces.
    /// </remarks>
    public interface IScopePortal : IScope
    {
    }
}
