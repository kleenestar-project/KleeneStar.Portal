using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebPage;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// View container for the "Organization" page. Hosts the search, quickfilter,
    /// table, and pagination sub-fragments so the issue list mirrors the operator-side
    /// workspace overview (search field, quickfilter, paging).
    /// </summary>
    [Section<SectionContentPrimary>]
    [Scope<global::KleeneStar.Portal.WWW.Org.Index>]
    [Cache]
    public sealed class PortalOrgViewFragment : FragmentControlView
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalOrgViewFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            Layout = _ => TypeLayoutView.ToggleGroup;
        }

        /// <summary>
        /// Convert the fragment to HTML.
        /// </summary>
        /// <param name="renderContext">The context in which the fragment is rendered.</param>
        /// <param name="visualTree">The visual tree used for rendering the fragment.</param>
        /// <returns>An HTML node representing the rendered fragments. Can be null if no nodes are present.</returns>
        public override IHtmlNode Render(IRenderControlContext renderContext, IVisualTreeControl visualTree)
        {
            return base.Render(renderContext, visualTree);
        }
    }
}
