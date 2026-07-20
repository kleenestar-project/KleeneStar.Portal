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
    /// Provides the search field of the "Organization" view. The advanced search
    /// control feeds the issue table's <c>q</c> (substring) query through the
    /// <c>BindSearch</c> binding declared on
    /// <see cref="PortalOrgViewTableFragment"/>.
    /// </summary>
    [Section<SectionViewHeaderPrimary>]
    [Scope<PortalOrgViewFragment>]
    [Cache]
    public sealed class PortalOrgViewSearchFragment : FragmentControlViewHeader
    {
        /// <summary>
        /// Represents the unique identifier for the search content. Referenced by the
        /// table fragment's <c>BindSearch.Source</c>.
        /// </summary>
        public static readonly string ContentId = "id_8C6F4D2B0A5E7193AD3F4051627384A0";

        /// <summary>
        /// Gets the search control used to query and filter the issue list.
        /// </summary>
        public ControlAdvancedSearch Search { get; } = new ControlAdvancedSearch(ContentId)
        {
            ServiceFactory = _ => DataServiceDescriptor.QueryData(PortalHub.GetUri<global::KleeneStar.Portal.WWW.Api._1_.Issues.Org.Wql>().ToString())
        };

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalOrgViewSearchFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            Add(Search);
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
