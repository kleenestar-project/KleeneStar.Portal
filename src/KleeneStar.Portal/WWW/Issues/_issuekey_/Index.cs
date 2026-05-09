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

namespace KleeneStar.Portal.WWW.Issues._issuekey_
{
    /// <summary>
    /// Issue detail page — shows the description, the conversation timeline, and a side
    /// panel with participants. Mirrors the "Issue Detail (Drawer)" mockup in
    /// <c>kleenestar.portal.md</c>.
    /// </summary>
    [WebIcon<IssueIcon>]
    [IssueKeySegment]
    [Scope<IScopePortal>]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
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
            var keyParameter = renderContext.Request.GetParameter<IssueKeyParameter>();
            var issue = _portalManager.GetIssue(keyParameter?.Value);

            visualTree.Title = issue?.Title ?? keyParameter?.Value;
            visualTree.Content.MainPanel.Headline.Title = issue?.Title ?? keyParameter?.Value;

            if (issue is null)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => $"Vorgang '{keyParameter?.Value}' wurde nicht gefunden oder ist nicht für dich freigegeben.",
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            // header strip — key, status, priority, request type, requester, assignee
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => $"{issue.Key} · {FormatPortalState(issue.PortalState)} · {issue.Priority} · {issue.RequestTypeName} · von {issue.Requester?.Name ?? "—"} · zugewiesen an {issue.AssigneeLabel}",
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
            });

            // resolution-acceptance banner — only when state == Resolved
            if (issue.PortalState == PortalIssueState.Resolved)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => "Lösung vorgeschlagen. Bitte prüfen und bestätigen, dass dein Anliegen behoben ist.",
                    TextColor = _ => new PropertyColorText(TypeColorText.Primary),
                    Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
                });
            }

            // description
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => "BESCHREIBUNG",
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => issue.Description ?? string.Empty,
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
            });

            // history / timeline
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => $"VERLAUF · {issue.Comments.Count} {(issue.Comments.Count == 1 ? "Eintrag" : "Einträge")}",
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
                var capturedComment = comment;
                var bodyText = comment.Text;

                timeline = timeline.AddRow
                (
                    new ControlTableCell()
                    {
                        Text = _ =>
                        {
                            var who = capturedComment.IsSystem
                                ? "System"
                                : (capturedComment.Author?.Name ?? "—");
                            var role = string.IsNullOrEmpty(capturedComment.Role)
                                ? string.Empty
                                : $" · {capturedComment.Role}";
                            var when = FormatRelative(capturedComment.Timestamp);

                            return $"{who}{role} · {when}";
                        }
                    },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => bodyText })
                );
            }
            visualTree.Content.MainPanel.AddPrimary(timeline);

            // side panel — details, requester, shared, watchers
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => "DETAILS",
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Four, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });

            var details = new ControlTable() { Striped = _ => TypeStripedTable.Row, SuppressHeaders = _ => true }
                .AddColumn("")
                .AddColumn("")
                .AddRow
                (
                    new ControlTableCell() { Text = _ => "Typ" },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => issue.RequestTypeName })
                )
                .AddRow
                (
                    new ControlTableCell() { Text = _ => "Bearbeitet von" },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => issue.AssigneeLabel })
                )
                .AddRow
                (
                    new ControlTableCell() { Text = _ => "Erstellt" },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => issue.Created.ToString("yyyy-MM-dd") })
                )
                .AddRow
                (
                    new ControlTableCell() { Text = _ => "Aktualisiert" },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => FormatRelative(issue.Updated) })
                );
            if (issue.RequiresApproval)
            {
                details = details.AddRow
                (
                    new ControlTableCell() { Text = _ => "Hinweis" },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => "Freigabe erforderlich" })
                );
            }
            visualTree.Content.MainPanel.AddPrimary(details);

            // shared with
            if (issue.SharedWith.Count > 0)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => $"GETEILT MIT ({issue.SharedWith.Count})",
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
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => $"BEOBACHTER ({issue.Watchers.Count})",
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                    Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Three, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
                });
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => string.Join(", ", issue.Watchers.Select(p => p.Name))
                });
            }
        }

        private static string FormatPortalState(PortalIssueState state) => state switch
        {
            PortalIssueState.Open => "Offen",
            PortalIssueState.InProgress => "In Bearbeitung",
            PortalIssueState.WaitingOnRequester => "Wartet auf mich",
            PortalIssueState.Resolved => "Gelöst",
            PortalIssueState.Closed => "Geschlossen",
            _ => state.ToString()
        };

        private static string FormatRelative(DateTime timestamp)
        {
            var delta = DateTime.UtcNow - timestamp;
            if (delta.TotalMinutes < 60) { return $"vor {Math.Max(1, (int)delta.TotalMinutes)} Min."; }
            if (delta.TotalHours < 24) { return $"vor {(int)delta.TotalHours} Std."; }
            if (delta.TotalDays < 7) { return $"vor {(int)delta.TotalDays} Tagen"; }
            return $"vor {(int)(delta.TotalDays / 7)} Wochen";
        }
    }
}
