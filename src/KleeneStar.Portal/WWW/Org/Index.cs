using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW.Org
{
    /// <summary>
    /// "Organisation" page — lists all issues inside the calling identity's tenant the
    /// active permission profile permits them to see.
    /// </summary>
    /// <remarks>
    /// The issue list itself (search field, quickfilter, table, pagination) is rendered
    /// by the view fragments scoped to this page (<c>PortalOrgViewFragment</c> and its
    /// children), mirroring the operator-side workspace overview. This page only
    /// contributes the headline and the introductory description.
    /// </remarks>
    [WebIcon<IssueIcon>]
    [Title("kleenestar.portal:org.title")]
    [Scope<IScopePortal>]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Index()
        {
        }

        /// <summary>
        /// Processing of the resource.
        /// </summary>
        /// <param name="renderContext">The context for rendering the page.</param>
        /// <param name="visualTree">The visual tree of the web application.</param>
        public void Process(IRenderContext renderContext, VisualTreeWebApp visualTree)
        {
            visualTree.Title = "kleenestar.portal:org.title";
            visualTree.Content.MainPanel.Headline.Title = "kleenestar.portal:org.headline";

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => "kleenestar.portal:org.description",
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
            });
        }
    }
}
