using KleeneStar.Core;
using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebPage;
using WebExpress.WebApp.WebScope;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;

namespace KleeneStar.Portal.WWW.Session
{
    /// <summary>
    /// Portal login page — the focused authentication entry point. Supports password-based
    /// authentication and federated sign-in via the tenant's configured SSO providers.
    /// Intentionally free of operator-side branding so the customer perceives this as
    /// <em>their</em> service portal.
    /// </summary>
    /// <remarks>
    /// Authentication is delegated to the shared session endpoint exposed by
    /// <see cref="KleeneStar.Core.WWW.Api._1_.Session"/> — the portal identity model is the
    /// same as the operator-side WebApp.
    /// </remarks>
    [WebIcon<PortalIcon>]
    [SegmentHidden]
    [Title("kleenestar.portal:login.title")]
    [Scope<IScopePortal>]
    [Scope<IScopeLogin>]
    [Cache]
    public sealed class Index : PageWebAppLogin, IScopePortal, IScopeLogin
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Index()
        {
        }

        /// <summary>
        /// Processing of the resource.
        /// </summary>
        /// <param name="renderContext">The context for rendering the page.</param>
        /// <param name="visualTree">The visual tree of the web application login.</param>
        public override void Process(IRenderContext renderContext, VisualTreeWebAppLogin visualTree)
        {
            visualTree.LoginUri = CoreHub.GetUri<global::KleeneStar.Core.WWW.Api._1_.Session>();
        }
    }
}
