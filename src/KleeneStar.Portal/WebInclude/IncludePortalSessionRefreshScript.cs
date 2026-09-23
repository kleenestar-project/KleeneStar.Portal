using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebInclude;

namespace KleeneStar.Portal.WebInclude
{
    /// <summary>
    /// Includes the script that renews a sign-in's access token before it ends, on every portal
    /// page. The file is the core's own <c>sessionrefresh.js</c>, linked into the portal's
    /// assets by the project file - an include resolves its file among the assets of the plugin
    /// that declares it.
    /// </summary>
    [Asset("/assets/js/sessionrefresh.js")]
    public sealed class IncludePortalSessionRefreshScript : IInclude
    {
    }
}
