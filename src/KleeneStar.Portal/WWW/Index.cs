using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.Internationalization;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebIcon;

namespace KleeneStar.Portal.WWW
{
    /// <summary>
    /// Portal home page — the primary landing experience after login. Headline is set
    /// here; the request-type catalog and the caller's recent issues are rendered by
    /// <see cref="WebFragment.PortalHomeRequestTypesFragment"/> and
    /// <see cref="WebFragment.PortalHomeRecentFragment"/>, both bound to this page via
    /// <see cref="ScopeAttribute{T}"/>. Mirrors the "Portal Home" mockup in
    /// <c>kleenestar.portal.md</c>.
    /// </summary>
    [WebIcon<PortalIcon>]
    [Title("kleenestar.portal:home.title")]
    [Scope<IScopePortal>]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>
        /// Processing of the resource.
        /// </summary>
        /// <param name="renderContext">The context for rendering the page.</param>
        /// <param name="visualTree">The visual tree of the web application.</param>
        public void Process(IRenderContext renderContext, VisualTreeWebApp visualTree)
        {
            visualTree.Title = I18N.Translate(renderContext, "kleenestar.portal:home.title");
            visualTree.Content.MainPanel.Headline.Title = I18N.Translate(renderContext, "kleenestar.portal:home.headline");
        }
    }
}
