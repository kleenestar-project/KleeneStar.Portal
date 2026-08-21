using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebCore.WebIcon;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebIcon;
using WebExpress.WebUI.WebPage;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// "Workspaces" navigation item — links to the portal administration
    /// landing page (<see cref="global::KleeneStar.Portal.WWW.Workspaces.Index"/>),
    /// which lists the workspaces the calling identity is allowed to administer.
    /// </summary>
    [Section<SectionAppNavigationPrimary>]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class PortalAdminNavFragment : FragmentControlDropdownItemLink
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The fragment context.</param>
        public PortalAdminNavFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            Text = _ => "kleenestar.portal:nav.admin.label";
            Icon = _ => new IconSitemap();
        }

        /// <summary>
        /// Convert the control to HTML.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        /// <param name="visualTree">The visual tree.</param>
        /// <returns>The rendered link.</returns>
        public override IHtmlNode Render(IRenderControlContext renderContext, IVisualTreeControl visualTree)
        {
            Uri = _ => PortalHub.GetUri<global::KleeneStar.Portal.WWW.Workspaces.Index>();
            return base.Render(renderContext, visualTree);
        }
    }
}
