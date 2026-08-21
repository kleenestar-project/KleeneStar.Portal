using KleeneStar.Core.WebControl;
using WebExpress.WebApp.WebControl;
using WebExpress.WebApp.WebData;
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
    /// Provides the table of the "My Issues" view. The REST table is fed by the
    /// portal issue endpoint and is bound to the search, quickfilter, and
    /// pagination controls so it re-queries whenever the user types, toggles a
    /// filter chip, or pages.
    /// </summary>
    [Section<SectionViewItemPrimary>]
    [Scope<PortalMineViewFragment>]
    [Cache]
    public sealed class PortalMineViewTableFragment : FragmentControlViewItem
    {
        /// <summary>
        /// The id of the table. It is fixed rather than generated, because the empty-state
        /// placeholder names the table it belongs to.
        /// </summary>
        public const string TableId = "portal-mine-issues-table";

        /// <summary>
        /// Resource key of the message shown while the view has no matching issues.
        /// </summary>
        public const string EmptyResource = "kleenestar.portal:mine.empty";

        /// <summary>
        /// Gets the table that displays the calling identity's issues.
        /// </summary>
        public ControlDataTable Table { get; } = new ControlDataTable(TableId)
        {
            ServiceFactory = _ => DataServiceDescriptor.TableData(PortalHub.GetUri<global::KleeneStar.Portal.WWW.Api._1_.Issues.Mine.Table>().ToString())
        };

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalMineViewTableFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            Icon = _ => new IconTable();
            Title = _ => "kleenestar.core:view.table.title";
            Table.Bind = _ => new Binding()
                .Add(new BindSearch() { Source = PortalMineViewSearchFragment.ContentId })
                .Add(new BindFilter())
                .Add(new BindPaging() { Source = PortalMineViewPaginationFragment.ContentId });

            Add(Table);

            // ControlDataTable paints an empty result as a bare header, so the "nothing here"
            // message is contributed as a sibling and toggled by the companion script
            Add(TableEmptyState.Create(TableId, EmptyResource));
        }

        /// <summary>
        /// Renders the control as an HTML node.
        /// </summary>
        /// <param name="renderContext">
        /// The context in which the control is rendered.
        /// </param>
        /// <param name="visualTree">
        /// The visual tree representing the control's structure.
        /// </param>
        /// <returns>
        /// An HTML node representing the rendered control.
        /// </returns>
        public override IHtmlNode Render(IRenderControlContext renderContext, IVisualTreeControl visualTree)
        {
            var script = TableEmptyState.Script;

            if (!string.IsNullOrEmpty(script))
            {
                visualTree.AddHeaderScript(script);
            }

            return base.Render(renderContext, visualTree);
        }
    }
}
