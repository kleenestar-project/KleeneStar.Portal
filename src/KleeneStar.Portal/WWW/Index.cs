using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebScope;
using System;
using System.Collections.Generic;
using System.Linq;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW
{
    /// <summary>
    /// Portal home page — the primary landing experience after login. Greets the user by
    /// first name, exposes the request-type catalog as a tile grid, and lists the user's
    /// recent issues. Mirrors the "Portal Home" mockup in <c>kleenestar.portal.md</c>.
    /// </summary>
    [WebIcon<PortalIcon>]
    [Title("kleenestar.portal:home.title")]
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
            var current = _portalManager.CurrentUser;
            _ = current?.Name?.Split(' ')[0] ?? string.Empty;

            visualTree.Title = "kleenestar.portal:home.title";
            visualTree.Content.MainPanel.Headline.Title = "kleenestar.portal:home.headline";

            // section header — request-type catalog
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => "kleenestar.portal:home.request-types.heading",
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Two, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });

            // request-type tile grid — one row of horizontally-stacked panels per request type
            var typeGrid = new ControlPanel("portal-request-types")
            {
                Direction = _ => TypeDirection.Horizontal
            };
            foreach (var requestType in _portalManager.GetRequestTypes())
            {
                typeGrid.Add(BuildRequestTypeTile(requestType));
            }
            visualTree.Content.MainPanel.AddPrimary(typeGrid);

            // section header — recent issues for the user
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => "kleenestar.portal:home.recent.heading",
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Four, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });

            var recent = _portalManager.GetIssues(IssueScope.Mine).Take(3).ToArray();
            if (recent.Length == 0)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => "kleenestar.portal:home.recent.empty",
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
            }
            else
            {
                visualTree.Content.MainPanel.AddPrimary(BuildIssueTable(recent));
            }
        }

        /// <summary>
        /// Builds a single request-type tile — title, description, template count.
        /// </summary>
        /// <param name="requestType">The request type to render.</param>
        /// <returns>A panel that visually represents the tile.</returns>
        private static ControlPanel BuildRequestTypeTile(IRequestType requestType)
        {
            var tile = new ControlPanel("rt-" + requestType.Key)
            {
                Direction = _ => TypeDirection.Vertical,
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.Two, PropertySpacing.Space.Two, PropertySpacing.Space.None)
            };

            tile.Add(new ControlText()
            {
                Text = _ => requestType.Title,
                TextColor = _ => new PropertyColorText(TypeColorText.Primary)
            });
            tile.Add(new ControlText()
            {
                Text = _ => requestType.Description,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
            });
            tile.Add(new ControlText()
            {
                Text = _ => $"{requestType.Templates.Count} Vorlagen",
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Format = _ => TypeFormatText.Code
            });
            return tile;
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
