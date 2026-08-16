using KleeneStar.Model.Entities;
using KleeneStar.Portal.WebAttribute;
using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebParameter;
using KleeneStar.Portal.WebScope;
using System;
using System.Linq;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.Internationalization;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW.Issues._issuekey_
{
    /// <summary>
    /// Issue detail page — shows the description, the conversation timeline, and a side
    /// panel with participants. Mirrors the "Issue Detail (Drawer)" mockup in
    /// <c>kleenestar.portal.md</c>.
    /// </summary>
    /// <remarks>
    /// Every string the page renders is composed here rather than handed to the controls
    /// as a resource key, because the headings and the header strip interpolate issue data
    /// into their text. The page therefore resolves the keys through
    /// <see cref="I18N.Translate(IRenderContext, string, object[])"/> itself and shares the
    /// state and timestamp wording with the list views through
    /// <see cref="PortalFormatter"/>.
    /// </remarks>
    [WebIcon<IssueIcon>]
    [IssueKeySegment]
    [Scope<IScopePortal>]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the message shown when the issue is not visible.</summary>
        public const string NotFoundResource = "kleenestar.portal:issue.not-found";

        /// <summary>Resource key for the header strip summarising the issue.</summary>
        public const string HeaderSummaryResource = "kleenestar.portal:issue.header.summary";

        /// <summary>Resource key for the banner shown while a resolution awaits confirmation.</summary>
        public const string ResolutionBannerResource = "kleenestar.portal:issue.resolution.banner";

        /// <summary>Resource key for the description heading.</summary>
        public const string DescriptionHeadingResource = "kleenestar.portal:issue.description.heading";

        /// <summary>Resource key for the history heading.</summary>
        public const string HistoryHeadingResource = "kleenestar.portal:issue.history.heading";

        /// <summary>Resource key for the singular noun of a history entry.</summary>
        public const string HistoryEntrySingularResource = "kleenestar.portal:issue.history.entry.singular";

        /// <summary>Resource key for the plural noun of a history entry.</summary>
        public const string HistoryEntryPluralResource = "kleenestar.portal:issue.history.entry.plural";

        /// <summary>Resource key for the author label of a machine-narrated entry.</summary>
        public const string SystemResource = "kleenestar.portal:issue.system";

        /// <summary>Resource key for the audience marker of an internal timeline entry.</summary>
        public const string InternalCommentResource = "kleenestar.portal:issue.comment.internal";

        /// <summary>Resource key for the details heading.</summary>
        public const string DetailsHeadingResource = "kleenestar.portal:issue.details.heading";

        /// <summary>Resource key for the request-type row label.</summary>
        public const string TypeLabelResource = "kleenestar.portal:issue.type.label";

        /// <summary>Resource key for the assignee row label.</summary>
        public const string AssigneeLabelResource = "kleenestar.portal:issue.assignee.label";

        /// <summary>Resource key for the creation row label.</summary>
        public const string CreatedLabelResource = "kleenestar.portal:issue.created.label";

        /// <summary>Resource key for the last-update row label.</summary>
        public const string UpdatedLabelResource = "kleenestar.portal:issue.updated.label";

        /// <summary>Resource key for the note row label.</summary>
        public const string NoteLabelResource = "kleenestar.portal:issue.note.label";

        /// <summary>Resource key for the approval-required note.</summary>
        public const string ApprovalRequiredResource = "kleenestar.portal:issue.approval-required.label";

        /// <summary>Resource key for the shared-with heading.</summary>
        public const string SharedWithHeadingResource = "kleenestar.portal:issue.shared-with.heading";

        /// <summary>Resource key for the watchers heading.</summary>
        public const string WatchersHeadingResource = "kleenestar.portal:issue.watchers.heading";

        /// <summary>Resource key for the placeholder shown in place of a missing value.</summary>
        public const string NotAvailableResource = "kleenestar.portal:common.na";

        /// <summary>Resource key of the " · {0}" separator used to append byline segments.</summary>
        private const string SeparatorResource = "kleenestar.portal:common.separator.value";

        private readonly IPortalManager _portalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="portalManager">The portal manager used to resolve the issue.</param>
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
            string Translate(string key) => I18N.Translate(renderContext, key);
            string Format(string key, params object[] args) => I18N.Translate(renderContext, key, args);

            var keyParameter = renderContext.Request.GetParameter<IssueKeyParameter>();
            var issue = _portalManager.GetIssue(keyParameter?.Value);

            visualTree.Title = issue?.Title ?? keyParameter?.Value;
            visualTree.Content.MainPanel.Headline.Title = issue?.Title ?? keyParameter?.Value;

            if (issue is null)
            {
                var notFound = Format(NotFoundResource, keyParameter?.Value ?? string.Empty);

                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => notFound,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            var na = Translate(NotAvailableResource);

            // header strip — key, status, priority, request type, requester, assignee
            var header = Format
            (
                HeaderSummaryResource,
                issue.Key,
                PortalFormatter.FormatPortalState(issue.PortalState, Translate),
                issue.Priority,
                issue.RequestTypeName,
                issue.Requester?.Name ?? na,
                issue.AssigneeLabel
            );

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => header,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
            });

            // resolution-acceptance banner — only when state == Resolved
            if (issue.PortalState == PortalIssueState.Resolved)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => ResolutionBannerResource,
                    TextColor = _ => new PropertyColorText(TypeColorText.Primary),
                    Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
                });
            }

            // description
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => DescriptionHeadingResource,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => issue.Description ?? string.Empty,
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
            });

            // history / timeline
            var entryNoun = Translate(issue.Comments.Count == 1 ? HistoryEntrySingularResource : HistoryEntryPluralResource);
            var historyHeading = Format(HistoryHeadingResource, issue.Comments.Count, entryNoun);

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => historyHeading,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });

            var timeline = new ControlTable()
            {
                Striped = _ => TypeStripedTable.Row,
                SuppressHeaders = _ => true
            }
                .AddColumn("")
                .AddColumn("");
            foreach (var comment in issue.Comments)
            {
                var bodyText = comment.Text;

                // the byline is resolved here rather than in the cell's lambda: the
                // translation needs the render context, which the lambda does not carry.
                var who = comment.IsSystem ? Translate(SystemResource) : (comment.Author?.Name ?? na);
                var role = string.IsNullOrEmpty(comment.Role) ? string.Empty : Format(SeparatorResource, comment.Role);
                var when = Format(SeparatorResource, PortalFormatter.FormatRelative(comment.Timestamp, Translate));

                // an entry the requester shares with the service team only is marked, so
                // the audience of what they are reading is never a guess
                var audience = string.Equals(comment.Visibility, CommentVisibilityExtensions.InternalTeamToken, StringComparison.OrdinalIgnoreCase)
                    ? Format(SeparatorResource, Translate(InternalCommentResource))
                    : string.Empty;

                var byline = $"{who}{role}{when}{audience}";

                timeline = timeline.AddRow
                (
                    new ControlTableCell() { Text = _ => byline },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => bodyText })
                );
            }
            visualTree.Content.MainPanel.AddPrimary(timeline);

            // side panel — details, requester, shared, watchers
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => DetailsHeadingResource,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Four, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });

            var updated = PortalFormatter.FormatRelative(issue.Updated, Translate);

            // ControlTableCell emits its text verbatim — unlike ControlText it does not resolve
            // a resource key — so the row labels are translated here. Handing it the key put
            // "kleenestar.portal:issue.type.label" on the page.
            var typeLabel = Translate(TypeLabelResource);
            var assigneeLabel = Translate(AssigneeLabelResource);
            var createdLabel = Translate(CreatedLabelResource);
            var updatedLabel = Translate(UpdatedLabelResource);

            var details = new ControlTable() { Striped = _ => TypeStripedTable.Row, SuppressHeaders = _ => true }
                .AddColumn("")
                .AddColumn("")
                .AddRow
                (
                    new ControlTableCell() { Text = _ => typeLabel },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => issue.RequestTypeName })
                )
                .AddRow
                (
                    new ControlTableCell() { Text = _ => assigneeLabel },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => issue.AssigneeLabel })
                )
                .AddRow
                (
                    new ControlTableCell() { Text = _ => createdLabel },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => issue.Created.ToString("yyyy-MM-dd") })
                )
                .AddRow
                (
                    new ControlTableCell() { Text = _ => updatedLabel },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => updated })
                );
            if (issue.RequiresApproval)
            {
                var noteLabel = Translate(NoteLabelResource);

                details = details.AddRow
                (
                    new ControlTableCell() { Text = _ => noteLabel },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => ApprovalRequiredResource })
                );
            }
            visualTree.Content.MainPanel.AddPrimary(details);

            // shared with
            if (issue.SharedWith.Count > 0)
            {
                var sharedHeading = Format(SharedWithHeadingResource, issue.SharedWith.Count);

                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => sharedHeading,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                    Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Three, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
                });
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => string.Join(", ", issue.SharedWith.Select(p => p.Name))
                });
            }

            // watchers
            if (issue.Watchers.Count > 0)
            {
                var watchersHeading = Format(WatchersHeadingResource, issue.Watchers.Count);

                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => watchersHeading,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                    Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Three, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
                });
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => string.Join(", ", issue.Watchers.Select(p => p.Name))
                });
            }
        }
    }
}
