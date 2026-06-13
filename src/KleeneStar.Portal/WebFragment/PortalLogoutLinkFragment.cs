using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebCondition;
using WebExpress.WebApp.WebFragment;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebComponent;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebCore.WebScope;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebPage;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Logout link in the user avatar dropdown of the portal. Visible only
    /// when a user is signed in; the logout endpoint is the shared
    /// <c>KleeneStar.Core.WWW.Api._1_.Session</c> so the operator and the
    /// portal share the same session lifecycle.
    /// </summary>
    [Section<SectionAppAvatarSecondary>]
    [Scope<IScopePortal>]
    [Condition<ConditionLogin>]
    [Cache]
    public sealed class PortalLogoutLinkFragment : FragmentControlDropdownItemLinkLogout
    {
        private readonly IComponentHub _componentHub;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="componentHub">The component hub used to manage components.</param>
        /// <param name="fragmentContext">The context in which the fragment is used.</param>
        public PortalLogoutLinkFragment(IComponentHub componentHub, IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            _componentHub = componentHub;
        }

        /// <summary>
        /// Convert the control to HTML.
        /// </summary>
        /// <param name="renderContext">The context in which the fragment is rendered.</param>
        /// <param name="visualTree">The visual tree representing the fragment's structure.</param>
        /// <returns>The rendered HTML node.</returns>
        public override IHtmlNode Render(IRenderControlContext renderContext, IVisualTreeControl visualTree)
        {
            var logoutUri = _componentHub.SitemapManager.GetUri<KleeneStar.Core.WWW.Api._1_.Session>(renderContext?.PageContext.ApplicationContext);

            return base.Render(renderContext, visualTree, logoutUri);
        }
    }
}
