using WebExpress.WebApp.WebControl;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebPage;
using WebExpress.WebUI.WebSection;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Provides the quickfilter control of the "Organization" view. The control offers
    /// the collapsed portal lifecycle states (Open, In Progress, Waiting, Resolved,
    /// Closed) as toggleable chips that drive the issue table's <c>f</c> filter
    /// parameter.
    /// </summary>
    [Section<SectionViewHeaderSecondary>]
    [Scope<PortalOrgViewFragment>]
    [Cache]
    public sealed class PortalOrgViewQuickfilterFragment : FragmentControlViewHeader
    {
        /// <summary>
        /// Represents the unique identifier for the quickfilter content.
        /// </summary>
        public static readonly string ContentId = "id_9D7A5E3C1B6F82A4BE405162738495B1";

        /// <summary>
        /// Gets the quick filter control for the REST-based issue query.
        /// </summary>
        public ControlRestQuickfilter Quickfilter { get; } = new ControlRestQuickfilter(ContentId)
        {
            RestUri = _ => PortalHub.GetUri<global::KleeneStar.Portal.WWW.Api._1_.Issues.Org.Quickfilter>()
        };

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalOrgViewQuickfilterFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            Add(Quickfilter);
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
