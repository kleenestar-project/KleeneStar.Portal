using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebParameter;
using System;
using System.Collections.Generic;
using System.Linq;
using WebExpress.WebApp.WebRestApi;
using WebExpress.WebCore.Internationalization;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Issues
{
    /// <summary>
    /// Shared projection logic behind the issue-list REST table endpoints
    /// (<c>Mine/Table</c> and <c>Org/Table</c>). Both endpoints differ only in the
    /// <see cref="IssueScope"/> they query, so the filtering, paging, column, and row
    /// shaping lives here and is reused by both leaf endpoints.
    /// </summary>
    /// <remarks>
    /// The issue list is a projection over the object model (creator/share/watch
    /// membership plus a workflow-derived portal state), so filtering and paging are
    /// applied in memory rather than pushed into a WebIndex query. The endpoints honour
    /// the same query parameters the operator-side workspace table uses: <c>q</c>
    /// (substring search over key/title), <c>f</c> (comma-separated quickfilter ids),
    /// <c>p</c> (zero-based page number), and <c>l</c> (page size).
    /// </remarks>
    public static class IssueTableProjection
    {
        /// <summary>
        /// The default page size used when the request carries no (or an invalid)
        /// <c>l</c> parameter.
        /// </summary>
        private const int DefaultPageSize = 50;

        /// <summary>
        /// Builds the filtered, paged page of the caller's issues for the given scope
        /// and returns it as a table result response.
        /// </summary>
        /// <param name="scope">The issue scope to query (<c>Mine</c> or <c>Organization</c>).</param>
        /// <param name="request">The incoming request.</param>
        /// <returns>The table result as a JSON response.</returns>
        public static IResponse Retrieve(IssueScope scope, IRequest request)
        {
            var pageNumber = Math.Max(0, ParseInt(request, "p", 0));
            var pageSize = ParseInt(request, "l", DefaultPageSize);
            if (pageSize <= 0)
            {
                pageSize = DefaultPageSize;
            }

            var search = request?.GetParameter("q")?.Value;
            var filters = request?.GetParameter("f")?.Value?
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? [];
            var states = ResolveStates(filters);

            var issues = PortalHub.PortalManager.GetIssues(scope).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search) && search != "null")
            {
                issues = issues.Where(i =>
                    (i.Key ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (i.Title ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (states.Count > 0)
            {
                issues = issues.Where(i => states.Contains(i.PortalState));
            }

            var filtered = issues.ToList();

            var rows = filtered
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .Select(i => BuildRow(i, request))
                .ToList();

            var result = new RestApiTableResult()
            {
                Title = null,
                Columns = BuildColumns(request),
                Rows = rows,
                Pagination = new RestApiPaginationInfo()
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = filtered.Count
                }
            };

            return result.ToResponse();
        }

        /// <summary>
        /// Builds the (fixed) column definitions of the issue table. The labels are
        /// translated manually — REST table labels are not auto-translated.
        /// </summary>
        /// <param name="request">The request used to resolve the localized labels.</param>
        /// <returns>The column definitions.</returns>
        private static IEnumerable<RestApiTableColumn> BuildColumns(IRequest request)
        {
            yield return new RestApiTableColumn()
            {
                Id = "key",
                Label = I18N.Translate(request, "kleenestar.portal:table.key"),
                Visible = true
            };

            yield return new RestApiTableColumn()
            {
                Id = "title",
                Label = I18N.Translate(request, "kleenestar.portal:table.title"),
                Visible = true
            };

            yield return new RestApiTableColumn()
            {
                Id = "status",
                Label = I18N.Translate(request, "kleenestar.portal:table.status"),
                Visible = true
            };

            yield return new RestApiTableColumn()
            {
                Id = "priority",
                Label = I18N.Translate(request, "kleenestar.portal:table.priority"),
                Visible = true
            };

            yield return new RestApiTableColumn()
            {
                Id = "updated",
                Label = I18N.Translate(request, "kleenestar.portal:table.updated"),
                Visible = true
            };
        }

        /// <summary>
        /// Projects a single issue to a table row, linking the row to the issue detail
        /// page.
        /// </summary>
        /// <param name="issue">The issue to project.</param>
        /// <param name="request">The request used to resolve the localized cell content.</param>
        /// <returns>The table row.</returns>
        private static RestApiTableRow BuildRow(IIssue issue, IRequest request)
        {
            string Translate(string key) => I18N.Translate(request, key);

            var uri = PortalHub.GetUri<global::KleeneStar.Portal.WWW.Issues._issuekey_.Index>()?
                .BindParameters(new IssueKeyParameter(issue.Key));

            return new RestApiTableRow()
            {
                Id = issue.Key,
                Cells =
                [
                    new RestApiTableCell() { Content = issue.Key },
                    new RestApiTableCell() { Content = issue.Title },
                    new RestApiTableCell() { Content = PortalFormatter.FormatPortalState(issue.PortalState, Translate) },
                    new RestApiTableCell() { Content = issue.Priority },
                    new RestApiTableCell() { Content = PortalFormatter.FormatRelative(issue.Updated, Translate) }
                ],
                Uri = uri?.ToString()
            };
        }

        /// <summary>
        /// Maps the selected quickfilter ids to the set of portal states they represent.
        /// Unknown ids are ignored.
        /// </summary>
        /// <param name="filters">The selected quickfilter ids.</param>
        /// <returns>The set of states to keep, empty when no state chip is selected.</returns>
        private static HashSet<PortalIssueState> ResolveStates(IEnumerable<string> filters)
        {
            var states = new HashSet<PortalIssueState>();

            foreach (var filter in filters)
            {
                switch (filter.ToLowerInvariant())
                {
                    case IssueQuickfilterBase.OpenId:
                        states.Add(PortalIssueState.Open);
                        break;
                    case IssueQuickfilterBase.InProgressId:
                        states.Add(PortalIssueState.InProgress);
                        break;
                    case IssueQuickfilterBase.WaitingId:
                        states.Add(PortalIssueState.WaitingOnRequester);
                        break;
                    case IssueQuickfilterBase.ResolvedId:
                        states.Add(PortalIssueState.Resolved);
                        break;
                    case IssueQuickfilterBase.ClosedId:
                        states.Add(PortalIssueState.Closed);
                        break;
                    default:
                        continue;
                }
            }

            return states;
        }

        /// <summary>
        /// Parses an integer request parameter, falling back to a default when the
        /// parameter is missing or not a number.
        /// </summary>
        /// <param name="request">The request carrying the parameter.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="fallback">The value returned when parsing fails.</param>
        /// <returns>The parsed value, or <paramref name="fallback"/>.</returns>
        private static int ParseInt(IRequest request, string name, int fallback)
        {
            var raw = request?.GetParameter(name)?.Value;

            return int.TryParse(raw, out var value) ? value : fallback;
        }
    }
}
