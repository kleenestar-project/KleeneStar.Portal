using WebExpress.WebApp.WebControl;
using WebExpress.WebApp.WebData;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebPage;
using WebExpress.WebUI.WebSection;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Provides the quickfilter control of the "My Issues" view. The control
    /// offers the collapsed portal lifecycle states (Open, In Progress, Waiting,
    /// Resolved, Closed) as toggleable chips that drive the issue table's
    /// <c>f</c> filter parameter.
    /// </summary>
    [Section<SectionViewHeaderSecondary>]
    [Scope<PortalMineViewFragment>]
    [Cache]
    public sealed class PortalMineViewQuickfilterFragment : FragmentControlViewHeader
    {
        /// <summary>
        /// Represents the unique identifier for the quickfilter content.
        /// </summary>
        public static readonly string ContentId = "id_6A4D2B0F8C3E5F7A9B1D2E3F40516273";

        /// <summary>
        /// Gets the quick filter control for the REST-based issue query.
        /// </summary>
        public ControlDataQuickfilter Quickfilter { get; } = new ControlDataQuickfilter(ContentId)
        {
            ServiceFactory = _ => DataServiceDescriptor.QueryData(PortalHub.GetUri<global::KleeneStar.Portal.WWW.Api._1_.Issues.Mine.Quickfilter>().ToString())
        };

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalMineViewQuickfilterFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            Add(Quickfilter);
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
            return base.Render(renderContext, visualTree);
        }
    }
}
