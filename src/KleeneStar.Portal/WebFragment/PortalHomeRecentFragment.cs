using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebManager;
using System.Linq;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.Internationalization;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebPage;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Renders the "recently by you" section on the portal home page. Headline plus either
    /// an empty-state hint or the standard issue table populated with the caller's most
    /// recent issues (capped at three rows).
    /// </summary>
    [Section<SectionContentPrimary>]
    [Scope<WWW.Index>]
    [Cache]
    public sealed class PortalHomeRecentFragment : FragmentControlPanel
    {
        private readonly IPortalManager _portalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The fragment context.</param>
        /// <param name="portalManager">The portal manager.</param>
        public PortalHomeRecentFragment(IFragmentContext fragmentContext, IPortalManager portalManager)
            : base(fragmentContext)
        {
            _portalManager = portalManager;

            Add(new ControlText()
            {
                Text = _ => "kleenestar.portal:home.recent.heading",
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Four, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });

            var recent = _portalManager.GetIssues(IssueScope.Mine).Take(3).ToArray();
            if (recent.Length == 0)
            {
                Add(new ControlText()
                {
                    Text = _ => "kleenestar.portal:home.recent.empty",
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
            }
            else
            {
                Add(PortalFormatter.BuildIssueTable(recent, TranslateKey));
            }
        }

        /// <summary>
        /// Resolves a portal resource key to its localized text. Indirection so the
        /// formatter (which only needs a key→string function) can be reused outside the
        /// render context without pulling in WebCore.Internationalization directly.
        /// </summary>
        /// <param name="key">The resource key to translate.</param>
        /// <returns>The localized string.</returns>
        private static string TranslateKey(string key) => I18N.Translate(key);

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
