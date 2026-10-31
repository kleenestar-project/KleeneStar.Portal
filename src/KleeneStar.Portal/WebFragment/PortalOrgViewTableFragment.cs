using WebExpress.WebApp.WebControl;
using WebExpress.WebApp.WebData;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebIcon;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebIcon;
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
        /// The stable identifier of the issue table.
        /// </summary>
        public const string TableId = "portal-org-issues-table";

        /// <summary>
        /// Resource key of the message shown while the view has no matching issues.
        /// </summary>
        public const string EmptyResource = "kleenestar.portal:org.empty";

        /// <summary>
        /// Gets the table that displays the organization's issues.
        /// </summary>
        public ControlDataTable Table { get; } = new ControlDataTable(TableId)
        {
            ServiceFactory = _ => DataServiceDescriptor.TableData(PortalHub.GetUri<global::KleeneStar.Portal.WWW.Api._1_.Issues.Org.Table>().ToString())
        };

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalOrgViewTableFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            // the table is the view here rather than a block among others, so it takes the
            // height it is handed instead of growing with its rows: the rows then scroll under
            // a column header that stays, and the pager stays in reach below them
            Table.Fill = _ => true;
            Table.EmptyText = _ => EmptyResource;

            Icon = _ => new IconTable();
            Title = _ => "kleenestar.core:view.table.title";
            Table.Bind = _ => new Binding()
                .Add(new BindSearch() { Source = PortalOrgViewSearchFragment.ContentId })
                .Add(new BindFilter())
                .Add(new BindPaging() { Source = PortalOrgViewPaginationFragment.ContentId });

            Add(Table);


        }

    }
}
