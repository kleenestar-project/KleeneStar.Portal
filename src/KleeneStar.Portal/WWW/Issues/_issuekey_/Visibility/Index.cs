using KleeneStar.Core.WebControl;
using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebParameter;
using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW.Issues._issuekey_.Visibility
{
    /// <summary>
    /// Visibility page - says who sees an issue and lets its requester change that: kept to
    /// its participants (the requester and the colleagues it was shared with), or visible to
    /// the whole organization.
    /// </summary>
    /// <remarks>
    /// The page offers the one change that applies: restricting an open issue, or opening a
    /// restricted one. The form tells the endpoint which way through a hidden field, because a
    /// form submits by <c>POST</c> only. Whether the caller may change it is the manager's
    /// question; a caller who may not is refused by the endpoint.
    /// </remarks>
    [WebIcon<IssueIcon>]
    [Scope<IScopePortal>]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the headline.</summary>
        public const string HeadlineResource = "kleenestar.portal:visibility.headline";

        /// <summary>Resource key for the description of a restricted issue.</summary>
        public const string RestrictedResource = "kleenestar.portal:visibility.restricted";

        /// <summary>Resource key for the description of an issue the organization sees.</summary>
        public const string OrganizationResource = "kleenestar.portal:visibility.organization";

        /// <summary>Resource key for the not-found message.</summary>
        public const string NotFoundResource = "kleenestar.portal:visibility.not-found";

        /// <summary>Resource key for the button restricting the issue.</summary>
        public const string RestrictResource = "kleenestar.portal:visibility.restrict";

        /// <summary>Resource key for the button opening the issue to the organization.</summary>
        public const string OpenResource = "kleenestar.portal:visibility.open";

        /// <summary>Resource key for the cancel button.</summary>
        public const string CancelResource = "kleenestar.portal:visibility.cancel";

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

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => issue.Restricted ? RestrictedResource : OrganizationResource,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
            });

            var form = new ControlModalForm("portal-visibility-form")
            {
                Uri = _ => PortalHub.GetUri<global::KleeneStar.Portal.WWW.Api._1_.Issues._issuekey_.Restrict>()?.BindParameters(new IssueKeyParameter(keyParameter?.Value))
            };

            form.Add(new PresetHiddenInput("portal-visibility-restricted")
            {
                Name = _ => "restricted",
                Value = _ => issue.Restricted ? "false" : "true"
            });
            form.AddPrimaryButton(new ControlFormItemButtonSubmit() { Text = _ => issue.Restricted ? OpenResource : RestrictResource });
            form.AddPrimaryButton(new ControlFormItemButton() { Text = _ => CancelResource });
            visualTree.Content.MainPanel.AddPrimary(form);
        }
    }
}
