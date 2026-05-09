using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebIcon;
using WebExpress.WebUI.WebPage;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// "Start" navigation item — links to the portal home (<see cref="WWW.Index"/>).
    /// Mirrors the prototype's left-most topbar tab.
    /// </summary>
    [Section<SectionAppNavigationPrimary>]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class PortalHomeNavFragment : FragmentControlDropdownItemLink
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The fragment context.</param>
        public PortalHomeNavFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            Text = _ => "kleenestar.portal:nav.home.label";
            Icon = _ => new IconHouse();
        }

        /// <summary>
        /// Convert the control to HTML.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        /// <param name="visualTree">The visual tree.</param>
        /// <returns>The rendered link.</returns>
        public override IHtmlNode Render(IRenderControlContext renderContext, IVisualTreeControl visualTree)
        {
            Uri = _ => PortalHub.GetUri<WWW.Index>();
            return base.Render(renderContext, visualTree);
        }
    }
}
