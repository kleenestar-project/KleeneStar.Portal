using KleeneStar.Portal.WebAttribute;
using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebParameter;
using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW.Issues._issuekey_.Reject
{
    /// <summary>
    /// Reject-resolution page — the form that returns the issue to
    /// <see cref="PortalIssueState.InProgress"/> with a mandatory rejection reason.
    /// </summary>
    /// <remarks>
    /// The page lives at <c>/portal/issues/{key}/reject</c>; the concept document
    /// abbreviates the path to <c>/t/{key}/reject</c>.
    /// </remarks>
    [WebIcon<IssueIcon>]
    [IssueKeySegment]
    [Scope<IScopePortal>]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the headline.</summary>
        public const string HeadlineResource = "kleenestar.portal:reject.headline";

        /// <summary>Resource key for the description.</summary>
        public const string DescriptionResource = "kleenestar.portal:reject.description";

        /// <summary>Resource key for the reason input label.</summary>
        public const string ReasonLabelResource = "kleenestar.portal:reject.reason.label";

        /// <summary>Resource key for the not-found message.</summary>
        public const string NotFoundResource = "kleenestar.portal:reject.not-found";

        /// <summary>Resource key for the conflict message shown when no resolution is pending.</summary>
        public const string ConflictResource = "kleenestar.portal:reject.conflict";

        /// <summary>Resource key for the submit button.</summary>
        public const string SubmitResource = "kleenestar.portal:reject.submit";

        /// <summary>Resource key for the cancel button.</summary>
        public const string CancelResource = "kleenestar.portal:reject.cancel";

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
            var keyParameter = renderContext.Request.GetParameter<IssueKeyParameter>();
            var issue = _portalManager.GetIssue(keyParameter?.Value);

            visualTree.Title = HeadlineResource;
            visualTree.Content.MainPanel.Headline.Title = HeadlineResource;

            if (issue is null)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => NotFoundResource,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            if (issue.PortalState != PortalIssueState.Resolved)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => ConflictResource,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => DescriptionResource,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
            });

            var form = new ControlModalForm("portal-reject-form")
            {
                Uri = _ => PortalHub.GetUri<global::KleeneStar.Portal.WWW.Api._1_.Issues._issuekey_.Reject>()?.BindParameters(new IssueKeyParameter(keyParameter?.Value))
            };

            form.Add(new ControlFormItemInputText("reason")
            {
                Label = _ => ReasonLabelResource,
                Required = _ => true
            });

            form.AddPrimaryButton(new ControlFormItemButtonSubmit() { Text = _ => SubmitResource });
            form.AddPrimaryButton(new ControlFormItemButton() { Text = _ => CancelResource });
            visualTree.Content.MainPanel.AddPrimary(form);
        }
    }
}
