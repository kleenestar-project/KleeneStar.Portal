using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebParameter;
using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.Internationalization;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebCore.WebIcon;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebPage;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Renders the request-type catalog on the portal home page as a card grid.
    /// Each <see cref="IRequestType"/> is shown as a <see cref="ControlTileCard"/>
    /// with header, icon (sourced from <c>Class.Icon</c> via the projection), and
    /// a click-through to the template picker.
    /// </summary>
    [Section<SectionContentPrimary>]
    [Scope<WWW.Index>]
    [Cache]
    public sealed class PortalHomeRequestTypesFragment : FragmentControlPanel
    {
        private readonly IPortalManager _portalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The fragment context.</param>
        /// <param name="portalManager">The portal manager.</param>
        public PortalHomeRequestTypesFragment(IFragmentContext fragmentContext, IPortalManager portalManager)
            : base(fragmentContext)
        {
            _portalManager = portalManager;

            Add(new ControlText()
            {
                Text = _ => "kleenestar.portal:home.request-types.heading",
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Two, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });

            var grid = new ControlTile("portal-request-types");
            foreach (var requestType in _portalManager.GetRequestTypes())
            {
                grid.Add(BuildRequestTypeCard(requestType));
            }

            Add(grid);
        }

        /// <summary>
        /// Builds a single request-type card — header (title), icon (from
        /// <see cref="IRequestType.Icon"/>; <see langword="null"/> renders no
        /// icon), a description line, and a click-through to the template picker.
        /// </summary>
        /// <param name="requestType">The request type to render.</param>
        /// <returns>A tile card that represents the request type.</returns>
        private static IControlTileCard BuildRequestTypeCard(IRequestType requestType)
        {
            var card = new ControlTileCard("rt-" + requestType.Key)
            {
                Header = _ => requestType.Title,
                Icon = requestType.Icon is null
                    ? null
                    : (_ => requestType.Icon),
                PrimaryAction = _ => new ActionModal
                (
                    "modal-request-type",
                    PortalHub.GetUri<WWW.Types._requesttypekey_.Index>()
                        ?.BindParameters(new RequestTypeKeyParameter(requestType.Key)),
                    TypeModalSize.Default
                )
            };

            card.Add(new ControlText()
            {
                Text = _ => requestType.Description,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
            });
            card.Add(new ControlText()
            {
                Text = _ => I18N.Translate("kleenestar.portal:request-type.templates.count", requestType.Templates.Count.ToString()),
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Format = _ => TypeFormatText.Code
            });

            return card;
        }

        /// <summary>
        /// Convert the fragment to HTML.
        /// </summary>
        /// <param name="renderContext">The context in which the fragment is rendered.</param>
        /// <param name="visualTree">The visual tree used for rendering the fragment.</param>
        /// <returns>The rendered HTML node.</returns>
        public override IHtmlNode Render(IRenderControlContext renderContext, IVisualTreeControl visualTree)
        {
            return base.Render(renderContext, visualTree);
        }
    }
}
