using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebCondition;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebIcon;
using WebExpress.WebUI.WebPage;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Login link in the user avatar dropdown of the portal. Visible only
    /// when the caller is signed out; opens the login dialog
    /// <see cref="PortalLoginModalFragment"/> renders with the page, so the
    /// user stays in the current context.
    /// </summary>
    /// <remarks>
    /// The dialog is on the page already, which is why the action names only
    /// its id and fetches nothing.
    /// </remarks>
    [Section<SectionAppAvatarSecondary>]
    [Scope<IScopePortal>]
    [Condition<ConditionLogout>]
    [Cache]
    public sealed class PortalLoginLinkFragment : FragmentControlDropdownItemLink
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context in which the fragment is used.</param>
        public PortalLoginLinkFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            Text = _ => "webexpress.webapp:login.label";
            Icon = _ => new IconRightToBracket();
            PrimaryAction = _ => new ActionModal("modal-login");
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
