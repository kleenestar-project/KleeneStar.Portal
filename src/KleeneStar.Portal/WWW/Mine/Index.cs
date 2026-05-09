using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebScope;
using System;
using System.Collections.Generic;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW.Mine
{
    /// <summary>
    /// "Meine Vorgänge" page — lists issues the calling identity created, was added to,
    /// or is watching. Mirrors the "Issue List" mockup in <c>kleenestar.portal.md</c>.
    /// </summary>
    [WebIcon<IssueIcon>]
    [Title("kleenestar.portal:mine.title")]
    [Scope<IScopePortal>]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
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
            visualTree.Title = "kleenestar.portal:mine.title";
            visualTree.Content.MainPanel.Headline.Title = "kleenestar.portal:mine.headline";

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => "kleenestar.portal:mine.description",
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
            });

            var issues = _portalManager.GetIssues(IssueScope.Mine);
            if (issues.Count == 0)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => "kleenestar.portal:mine.empty",
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            visualTree.Content.MainPanel.AddPrimary(BuildIssueTable(issues));
        }

        private static IControlTable BuildIssueTable(IEnumerable<IIssue> issues)
        {
            var table = new ControlTable()
            {
                Striped = _ => TypeStripedTable.Row
            }
                .AddColumn("kleenestar.portal:table.key")
                .AddColumn("kleenestar.portal:table.title")
                .AddColumn("kleenestar.portal:table.status")
                .AddColumn("kleenestar.portal:table.priority")
                .AddColumn("kleenestar.portal:table.updated");

            foreach (var issue in issues)
            {
                var capturedIssue = issue;

                table = table.AddRow
                (
                    new ControlTableCell() { Text = _ => capturedIssue.Key },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => capturedIssue.Title }),
                    new ControlTableCell() { Text = _ => FormatPortalState(capturedIssue.PortalState) },
                    new ControlTableCell() { Text = _ => capturedIssue.Priority },
                    new ControlTableCell() { Text = _ => FormatRelative(capturedIssue.Updated) }
                );
            }

            return table;
        }

        private static string FormatPortalState(PortalIssueState state) => state switch
        {
            PortalIssueState.Open => "kleenestar.portal:state.open",
            PortalIssueState.InProgress => "kleenestar.portal:state.in-progress",
            PortalIssueState.WaitingOnRequester => "kleenestar.portal:state.waiting",
            PortalIssueState.Resolved => "kleenestar.portal:state.resolved",
            PortalIssueState.Closed => "kleenestar.portal:state.closed",
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
