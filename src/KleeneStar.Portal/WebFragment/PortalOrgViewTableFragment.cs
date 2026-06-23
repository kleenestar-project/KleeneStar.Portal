using WebExpress.WebApp.WebControl;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebCore.WebIcon;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebIcon;
using WebExpress.WebUI.WebPage;
using WebExpress.WebUI.WebSection;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Provides the table of the "Organization" view. The REST table is fed by the
    /// portal issue endpoint and is bound to the search, quickfilter, and pagination
    /// controls so it re-queries whenever the user types, toggles a filter chip, or
    /// pages.
    /// </summary>
    [Section<SectionViewItemPrimary>]
    [Scope<PortalOrgViewFragment>]
    [Cache]
    public sealed class PortalOrgViewTableFragment : FragmentControlViewItem
    {
        /// <summary>
        /// Gets the table that displays the organization's issues.
        /// </summary>
        public ControlRestTable Table { get; } = new ControlRestTable()
        {
            RestUri = _ => PortalHub.GetUri<global::KleeneStar.Portal.WWW.Api._1_.Issues.Org.Table>()
        };

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalOrgViewTableFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            Icon = _ => new IconTable(TypeIconTheme.Light);
            Title = _ => "kleenestar.core:view.table.title";
            Table.Bind = _ => new Binding()
                .Add(new BindSearch() { Source = PortalOrgViewSearchFragment.ContentId })
                .Add(new BindFilter())
                .Add(new BindPaging() { Source = PortalOrgViewPaginationFragment.ContentId });

            Add(Table);
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
