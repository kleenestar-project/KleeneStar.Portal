using System;
using System.Collections.Generic;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WebDomain
{
    /// <summary>
    /// Pure formatting helpers shared by all portal pages and fragments. Translates the
    /// portal lifecycle states to German display labels, builds the standard issue table,
    /// and renders timestamps as coarse relative phrases.
    /// </summary>
    public static class PortalFormatter
    {
        /// <summary>
        /// Maps a <see cref="PortalIssueState"/> to its localized display label.
        /// </summary>
        /// <param name="state">The portal state.</param>
        /// <param name="translate">The translation function.</param>
        /// <returns>The display label.</returns>
        public static string FormatPortalState(PortalIssueState state, Func<string, string> translate) => state switch
        {
            PortalIssueState.Open => translate("kleenestar.portal:state.open"),
            PortalIssueState.InProgress => translate("kleenestar.portal:state.in-progress"),
            PortalIssueState.WaitingOnRequester => translate("kleenestar.portal:state.waiting"),
            PortalIssueState.Resolved => translate("kleenestar.portal:state.resolved"),
            PortalIssueState.Closed => translate("kleenestar.portal:state.closed"),
            _ => state.ToString()
        };

        /// <summary>
        /// Renders a timestamp as a coarse relative phrase ("vor 2 Std.", "vor 4 Tagen").
        /// </summary>
        /// <param name="timestamp">The timestamp to format.</param>
        /// <param name="translate">The translation function.</param>
        /// <returns>A short relative string.</returns>
        public static string FormatRelative(DateTime timestamp, Func<string, string> translate)
        {
            var delta = DateTime.UtcNow - timestamp;
            if (delta.TotalMinutes < 60) { return $"{translate("kleenestar.portal:relative.ago.prefix")} {Math.Max(1, (int)delta.TotalMinutes)} {translate("kleenestar.portal:relative.minutes")}"; }
            if (delta.TotalHours < 24) { return $"{translate("kleenestar.portal:relative.ago.prefix")} {(int)delta.TotalHours} {translate("kleenestar.portal:relative.hours")}"; }
            if (delta.TotalDays < 7) { return $"{translate("kleenestar.portal:relative.ago.prefix")} {(int)delta.TotalDays} {translate("kleenestar.portal:relative.days")}"; }
            return $"{translate("kleenestar.portal:relative.ago.prefix")} {(int)(delta.TotalDays / 7)} {translate("kleenestar.portal:relative.weeks")}";
        }

        /// <summary>
        /// Builds the standard issue table (key, title, status, priority, updated). Reused
        /// by the home page (recent issues), <c>Mine/Index</c>, and <c>Org/Index</c> so
        /// changes to the column layout stay in one place.
        /// </summary>
        /// <param name="issues">The issues to render.</param>
        /// <param name="translate">The translation function.</param>
        /// <returns>The populated table control.</returns>
        public static IControlTable BuildIssueTable(IEnumerable<IIssue> issues, Func<string, string> translate)
        {
            var table = new ControlTable()
            {
                Striped = _ => TypeStripedTable.Row
            }
                .AddColumn(translate("kleenestar.portal:table.key"))
                .AddColumn(translate("kleenestar.portal:table.title"))
                .AddColumn(translate("kleenestar.portal:table.status"))
                .AddColumn(translate("kleenestar.portal:table.priority"))
                .AddColumn(translate("kleenestar.portal:table.updated"));

            foreach (var issue in issues)
            {
                var capturedKey = issue.Key;
                var capturedTitle = issue.Title;
                var capturedState = FormatPortalState(issue.PortalState, translate);
                var capturedPriority = issue.Priority;
                var capturedUpdated = FormatRelative(issue.Updated, translate);

                table = table.AddRow
                (
                    new ControlTableCell() { Text = _ => capturedKey },
                    new ControlTableCellPanel().Add(new ControlText() { Text = _ => capturedTitle }),
                    new ControlTableCell() { Text = _ => capturedState },
                    new ControlTableCell() { Text = _ => capturedPriority },
                    new ControlTableCell() { Text = _ => capturedUpdated }
                );
            }

            return table;
        }
    }
}
