using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebCondition;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebComponent;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebCore.WebScope;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebIcon;
using WebExpress.WebUI.WebPage;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Profile link in the user avatar dropdown of the portal. Visible only
    /// when a user is signed in; targets the portal's profile page.
    /// </summary>
    [Section<SectionAppAvatarPrimary>]
    [Scope<IScopePortal>]
    [Condition<ConditionLogin>]
    [Cache]
    public sealed class PortalProfileLinkFragment : FragmentControlDropdownItemLink
    {
        private readonly IComponentHub _componentHub;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="componentHub">The component hub used to manage components.</param>
        /// <param name="fragmentContext">The context in which the fragment is used.</param>
        public PortalProfileLinkFragment(IComponentHub componentHub, IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            _componentHub = componentHub;
            Text = _ => "kleenestar.portal:profile.title";
            Icon = _ => new IconCircleUser();
            Uri = _ => PortalHub.GetUri<WWW.Profile.Index>();
        }

        /// <summary>
        /// Convert the control to HTML.
        /// </summary>
        /// <param name="renderContext">The context in which the fragment is rendered.</param>
        /// <param name="visualTree">The visual tree representing the fragment's structure.</param>
        /// <returns>The rendered HTML node.</returns>
        public override IHtmlNode Render(IRenderControlContext renderContext, IVisualTreeControl visualTree)
        {
            if (!FragmentContext.Conditions.Check(renderContext?.Request))
            {
                return null;
            }

            return base.Render(renderContext, visualTree);
        }
    }
}
