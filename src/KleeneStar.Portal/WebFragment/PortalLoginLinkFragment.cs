using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebCondition;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebComponent;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebCore.WebScope;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebIcon;
using WebExpress.WebUI.WebPage;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Login link in the user avatar dropdown of the portal. Visible only
    /// when the caller is signed out; opens the portal's login page as a
    /// modal so the user stays in the current context.
    /// </summary>
    [Section<SectionAppAvatarSecondary>]
    [Scope<IScopePortal>]
    [Condition<ConditionLogout>]
    [Cache]
    public sealed class PortalLoginLinkFragment : FragmentControlDropdownItemLink
    {
        private readonly IComponentHub _componentHub;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="componentHub">The component hub used to manage components.</param>
        /// <param name="fragmentContext">The context in which the fragment is used.</param>
        public PortalLoginLinkFragment(IComponentHub componentHub, IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            _componentHub = componentHub;
            Text = _ => "webexpress.webapp:login.label";
            Icon = _ => new IconRightToBracket();
            PrimaryAction = renderContext => new ActionModal
            (
                "modal-login",
                PortalHub.GetUri<WWW.Session.Index>(),
                TypeModalSize.Default
            );
        }

        /// <summary>
        /// Convert the control to HTML.
        /// </summary>
        /// <param name="renderContext">The context in which the fragment is rendered.</param>
        /// <param name="visualTree">The visual tree representing the fragment's structure.</param>
        /// <returns>The rendered HTML node.</returns>
        public override IHtmlNode Render(IRenderControlContext renderContext, IVisualTreeControl visualTree)
        {
            return base.Render(renderContext, visualTree);
        }
    }
}
