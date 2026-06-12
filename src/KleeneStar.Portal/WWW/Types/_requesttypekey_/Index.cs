using KleeneStar.Portal.WebAttribute;
using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebParameter;
using KleeneStar.Portal.WebScope;
using System.Linq;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW.Types._requesttypekey_
{
    /// <summary>
    /// Template picker — lists the templates available for a request type and
    /// offers a "blank request" escape. Mirrors the "Template Picker" mockup in
    /// <c>kleenestar.portal.md</c>.
    /// </summary>
    [WebIcon<RequestTypeIcon>]
    [RequestTypeKeySegment]
    [Scope<IScopePortal>]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the empty-state message.</summary>
        public const string NotAvailableResource = "kleenestar.portal:types.picker.unavailable";

        /// <summary>Resource key for the picker hint.</summary>
        public const string HintResource = "kleenestar.portal:types.picker.hint";

        /// <summary>Resource key for the blank-request label.</summary>
        public const string BlankRequestResource = "kleenestar.portal:types.picker.blank";

        /// <summary>Resource key for the blank-request hint.</summary>
        public const string BlankRequestHintResource = "kleenestar.portal:types.picker.blank.hint";

        private readonly IPortalManager _portalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="portalManager">The portal manager.</param>
        public Index(IPortalManager portalManager)
        {
            _portalManager = portalManager;
        }

        /// <summary>
        /// Processing of the resource.
        /// </summary>
        /// <param name="renderContext">The context for rendering the page.</param>
        /// <param name="visualTree">The visual tree of the web application.</param>
        public void Process(IRenderContext renderContext, VisualTreeWebApp visualTree)
        {
            var keyParameter = renderContext.Request.GetParameter<RequestTypeKeyParameter>();
            var requestType = _portalManager.GetRequestType(keyParameter?.Value);

            visualTree.Title = requestType?.Title ?? keyParameter?.Value;
            visualTree.Content.MainPanel.Headline.Title = requestType?.Title ?? keyParameter?.Value;

            if (requestType is null)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => NotAvailableResource,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => requestType.Description ?? string.Empty,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
            });

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => HintResource,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
            });

            var table = new ControlTable() { Striped = _ => TypeStripedTable.Row, SuppressHeaders = _ => true }
                .AddColumn("")
                .AddColumn("");

            var pathParam = new RequestTypeKeyParameter(keyParameter?.Value);

            foreach (var template in requestType.Templates)
            {
                var capturedKey = template.Key;
                var capturedTitle = template.Title;
                var capturedDescription = template.Description ?? string.Empty;

                table = table.AddRow
                (
                    new ControlTableCellPanel().Add(new ControlLink()
                    {
                        Text = _ => capturedTitle,
                        Uri = _ => PortalHub
                            .GetUri<New.Index>()
                            ?.BindParameters(pathParam)
                            ?.Add(new WebExpress.WebCore.WebUri.UriQuery("template", capturedKey))
                    }),
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => capturedDescription })
                );
            }

            // the blank-request escape hatch
            table = table.AddRow
            (
                new ControlTableCellPanel().Add(new ControlLink()
                {
                    Text = _ => BlankRequestResource,
                    Uri = _ => PortalHub.GetUri<New.Index>()?.BindParameters(pathParam)
                }),
                new ControlTableCellPanel().Add(new ControlText()
                {
                    Text = _ => BlankRequestHintResource,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                })
            );
            visualTree.Content.MainPanel.AddPrimary(table);
        }
    }
}
