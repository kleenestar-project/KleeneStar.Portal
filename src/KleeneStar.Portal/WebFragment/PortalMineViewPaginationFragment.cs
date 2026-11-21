using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebPage;
using WebExpress.WebUI.WebSection;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Provides the pagination footer of the "My Issues" view. The control's
    /// page state is bound to the issue table through the <c>BindPaging</c>
    /// binding declared on <see cref="PortalMineViewTableFragment"/>.
    /// </summary>
    [Section<SectionViewFooterPrimary>]
    [Scope<PortalMineViewFragment>]
    [Cache]
    public sealed class PortalMineViewPaginationFragment : FragmentControlViewFooter
    {
        /// <summary>
        /// Represents the unique identifier for the pagination content. Referenced
        /// by the table fragment's <c>BindPaging.Source</c>.
        /// </summary>
        public static readonly string ContentId = "id_7B5E3C1A9D4F60718C2E3F4051627384";

        /// <summary>
        /// Gets the pagination settings for controlling how the issue list is
        /// divided into pages.
        /// </summary>
        public ControlPagination Pagination { get; } = new ControlPagination(ContentId)
        {
        };

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalMineViewPaginationFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            Add(Pagination);
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
