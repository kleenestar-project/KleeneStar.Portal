using System;
using System.Collections.Generic;
using System.Globalization;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WebDomain
{
    /// <summary>
    /// Pure formatting helpers shared by all portal pages and fragments. Resolves the
    /// portal lifecycle states to their localized display labels, builds the standard
    /// issue table, and renders timestamps as coarse relative phrases. Every helper takes
    /// the translation function rather than reaching for the resource manager itself, so
    /// a caller outside a render context can supply its own.
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
        /// Renders a timestamp as a coarse relative phrase ("2 hrs. ago", "vor 4 Tagen").
        /// </summary>
        /// <remarks>
        /// Each bucket is one whole pattern rather than a shared "ago" token plus a unit,
        /// because the two languages put the marker on opposite sides of the number —
        /// composing them here produced "ago 2 hrs." in English.
        /// </remarks>
        /// <param name="timestamp">The timestamp to format.</param>
        /// <param name="translate">The translation function.</param>
        /// <returns>A short relative string.</returns>
        public static string FormatRelative(DateTime timestamp, Func<string, string> translate)
        {
            string Bucket(string key, int value)
            {
                return string.Format(CultureInfo.CurrentCulture, translate(key) ?? string.Empty, value);
            }

            var delta = DateTime.UtcNow - timestamp;

            if (delta.TotalMinutes < 60) { return Bucket("kleenestar.portal:relative.minutes", Math.Max(1, (int)delta.TotalMinutes)); }
            if (delta.TotalHours < 24) { return Bucket("kleenestar.portal:relative.hours", (int)delta.TotalHours); }
            if (delta.TotalDays < 7) { return Bucket("kleenestar.portal:relative.days", (int)delta.TotalDays); }

            return Bucket("kleenestar.portal:relative.weeks", (int)(delta.TotalDays / 7));
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
