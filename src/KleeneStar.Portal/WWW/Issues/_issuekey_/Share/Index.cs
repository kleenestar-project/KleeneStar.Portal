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

namespace KleeneStar.Portal.WWW.Issues._issuekey_.Share
{
    /// <summary>
    /// Share-issue page — the form that invites additional identities of the same
    /// tenant to read and comment on the issue. Mirrors the "Share Issue (Modal)"
    /// mockup in <c>kleenestar.portal.md</c>.
    /// </summary>
    /// <remarks>
    /// The page lives at <c>/portal/issues/{key}/share</c>; the concept document
    /// abbreviates the path to <c>/t/{key}/share</c>, but the existing portal code
    /// uses <c>issues</c> as the directory token (see the detail page at
    /// <c>WWW/Issues/_issuekey_/Index.cs</c>).
    /// </remarks>
    [WebIcon<IssueIcon>]
    [IssueKeySegment]
    [Scope<IScopePortal>]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the headline.</summary>
        public const string HeadlineResource = "kleenestar.portal:share.headline";

        /// <summary>Resource key for the invite-people label.</summary>
        public const string InviteLabelResource = "kleenestar.portal:share.invite.label";

        /// <summary>Resource key for the already-shared-with label.</summary>
        public const string AlreadySharedResource = "kleenestar.portal:share.already";

        /// <summary>Resource key for the shareable-link label.</summary>
        public const string LinkResource = "kleenestar.portal:share.link";

        /// <summary>Resource key for the not-found message.</summary>
        public const string NotFoundResource = "kleenestar.portal:share.not-found";

        /// <summary>Resource key for the submit button.</summary>
        public const string SubmitResource = "kleenestar.portal:share.submit";

        /// <summary>Resource key for the cancel button.</summary>
        public const string CancelResource = "kleenestar.portal:share.cancel";

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

            // issue sub-line: "INC-2041 · Outlook not receiving external mail"
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => $"{issue.Key} · {issue.Title}",
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
            });

            // the share form posts to the JSON REST endpoint of the portal.
            var form = new ControlModalForm("portal-share-form")
            {
                Uri = _ => PortalHub.GetUri<global::KleeneStar.Portal.WWW.Api._1_.Issues._issuekey_.Share>()?.BindParameters(new IssueKeyParameter(keyParameter?.Value))
            };

            var identities = new ControlFormItemInputSelection("identities")
            {
                Label = _ => InviteLabelResource,
                Required = _ => true,
                MultiSelect = _ => true
            };
            foreach (var member in _portalManager.GetOrganizationMembers().Where(m => !string.Equals(m.Id, _portalManager.CurrentUser?.Id, StringComparison.OrdinalIgnoreCase)))
            {
                identities.Add(new ControlFormItemInputSelectionItem(member.Id)
                {
                    Text = _ => member.Name
                });
            }
            form.Add(identities);

            form.AddPrimaryButton(new ControlFormItemButtonSubmit() { Text = _ => SubmitResource });
            form.AddPrimaryButton(new ControlFormItemButton() { Text = _ => CancelResource });
            visualTree.Content.MainPanel.AddPrimary(form);

            // the already-shared list
            if (issue.SharedWith.Count > 0)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => $"{AlreadySharedResource} ({issue.SharedWith.Count})",
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                    Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Three, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
                });

                var sharedTable = new ControlTable() { Striped = _ => TypeStripedTable.Row, SuppressHeaders = _ => true }
                    .AddColumn("")
                    .AddColumn("");
                foreach (var participant in issue.SharedWith)
                {
                    var capturedName = participant.Name;
                    var capturedEmail = participant.Email;
                    sharedTable = sharedTable.AddRow
                    (
                        new ControlTableCell() { Text = _ => capturedName },
                        new ControlTableCellPanel().Add(new ControlText()
                        {
                            Text = _ => capturedEmail,
                            TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                        })
                    );
                }
                visualTree.Content.MainPanel.AddPrimary(sharedTable);
            }

            // shareable link — points at the existing detail endpoint, so any portal
            // deployment can copy the URL into chat; opening it still requires
            // authentication and authorization on the recipient side.
            var issueUri = PortalHub.GetUri<global::KleeneStar.Portal.WWW.Issues._issuekey_.Index>()?.BindParameters(new IssueKeyParameter(keyParameter?.Value));
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => LinkResource,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Three, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });
            visualTree.Content.MainPanel.AddPrimary(new ControlLink()
            {
                Text = _ => issueUri?.ToString() ?? string.Empty,
                Uri = _ => issueUri
            });
        }
    }
}
