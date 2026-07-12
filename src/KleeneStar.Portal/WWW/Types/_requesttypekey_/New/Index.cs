using KleeneStar.Portal.WebAttribute;
using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebParameter;
using KleeneStar.Portal.WebScope;
using System;
using System.Linq;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW.Types._requesttypekey_.New
{
    /// <summary>
    /// Create-issue form — the page that gathers the title, details, and urgency
    /// for a new issue and submits it through the portal's JSON REST endpoint.
    /// Mirrors the "Create Issue (Modal)" mockup in <c>kleenestar.portal.md</c>.
    /// </summary>
    [WebIcon<RequestTypeIcon>]
    [RequestTypeKeySegment]
    [Scope<IScopePortal>]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the title input label.</summary>
        public const string TitleLabelResource = "kleenestar.portal:create.title.label";

        /// <summary>Resource key for the description input label.</summary>
        public const string DescriptionLabelResource = "kleenestar.portal:create.description.label";

        /// <summary>Resource key for the urgency input label.</summary>
        public const string UrgencyLabelResource = "kleenestar.portal:create.urgency.label";

        /// <summary>Resource key for the submit button.</summary>
        public const string SubmitResource = "kleenestar.portal:create.submit";

        /// <summary>Resource key for the cancel button.</summary>
        public const string CancelResource = "kleenestar.portal:create.cancel";

        /// <summary>Resource key for the request-type unavailable message.</summary>
        public const string NotAvailableResource = "kleenestar.portal:create.unavailable";

        /// <summary>Resource key for the priority prefix (P1..P4).</summary>
        public const string PriorityPrefixResource = "kleenestar.portal:create.priority";

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

            // optional ?template=… query parameter names the template to prefill;
            // the template only contributes its description to the issue body, see
            // PortalManager.CreateIssue.
            var templateKey = renderContext.Request.GetParameter("template")?.Value;
            var template = string.IsNullOrWhiteSpace(templateKey)
                ? null
                : requestType.Templates.FirstOrDefault(t =>
                    string.Equals(t.Key, templateKey, StringComparison.OrdinalIgnoreCase));

            if (template is not null)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => template.Title,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                if (!string.IsNullOrWhiteSpace(template.Description))
                {
                    visualTree.Content.MainPanel.AddPrimary(new ControlText()
                    {
                        Text = _ => template.Description,
                        TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                        Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
                    });
                }
            }

            // the create form posts to the JSON REST endpoint of the portal; the
            // control renders the standard layout (label, input, validation, submit).
            var form = new ControlModalForm("portal-create-issue-form")
            {
                Uri = _ => PortalHub.GetUri<global::KleeneStar.Portal.WWW.Api._1_.Issues.Index>()
            };

            form.Add(new ControlFormItemInputText("title")
            {
                Label = _ => TitleLabelResource,
                Required = _ => true
            });
            form.Add(new ControlFormItemInputText("description")
            {
                Label = _ => DescriptionLabelResource,
                Required = _ => false
            });

            var urgency = new ControlFormItemInputCombo("priority")
            {
                Label = _ => UrgencyLabelResource
            };
            urgency.Add
            (
                new ControlFormItemInputComboItem() { Value = _ => "P1", Text = _ => $"{PriorityPrefixResource} 1" },
                new ControlFormItemInputComboItem() { Value = _ => "P2", Text = _ => $"{PriorityPrefixResource} 2" },
                new ControlFormItemInputComboItem() { Value = _ => "P3", Text = _ => $"{PriorityPrefixResource} 3" },
                new ControlFormItemInputComboItem() { Value = _ => "P4", Text = _ => $"{PriorityPrefixResource} 4" }
            );
            form.Add(urgency);

            form.AddPrimaryButton(new ControlFormItemButtonSubmit() { Text = _ => SubmitResource });
            form.AddPrimaryButton(new ControlFormItemButton() { Text = _ => CancelResource });

            visualTree.Content.MainPanel.AddPrimary(form);
        }
    }
}
