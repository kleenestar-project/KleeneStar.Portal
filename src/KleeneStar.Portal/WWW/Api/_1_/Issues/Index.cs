using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebManager;
using System;
using System.Linq;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Issues
{
    /// <summary>
    /// REST endpoint over the portal issue collection. The URL is
    /// <c>/api/1/issues</c> below the portal application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>GET {base}?scope=mine|org&amp;q=…&amp;status=…</c> lists the issues visible
    /// to the calling identity. <c>scope</c> defaults to <c>mine</c>; <c>q</c> filters
    /// by key/title substring (case-insensitive); <c>status</c> filters by the
    /// camelCase portal state name (e.g. <c>waitingOnRequester</c>).
    /// </para>
    /// <para>
    /// <c>POST {base}</c> creates a new issue. The JSON body carries
    /// <c>requestTypeKey</c> (required), <c>title</c> (required), and optionally
    /// <c>templateKey</c>, <c>description</c>, and <c>priority</c> (<c>P1</c>–<c>P4</c>).
    /// A successful creation is acknowledged with <c>201 Created</c> and the issue
    /// projection; an unknown request type yields <c>404</c>, validation errors
    /// <c>400</c>.
    /// </para>
    /// </remarks>
    [Title("kleenestar.portal:api.issues.title")]
    [Cache]
    public sealed class Index : IRestApi
    {
        /// <summary>
        /// Gets the portal manager resolved through the <see cref="PortalHub"/> facade.
        /// REST endpoints are constructed by the framework without dependency
        /// injection, so the manager is reached the same way the existing core
        /// endpoints reach <c>CoreHub</c>.
        /// </summary>
        private static IPortalManager _portalManager => PortalHub.PortalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Index()
        {
        }

        /// <summary>
        /// Handles <c>GET {base}</c>: lists the issues of the requested scope with
        /// optional substring and state filters.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The issues as a JSON array (without timelines).</returns>
        [Method(RequestMethod.GET)]
        public IResponse Retrieve(IRequest request)
        {
            var scope = string.Equals(request?.GetParameter("scope")?.Value, "org", StringComparison.OrdinalIgnoreCase)
                ? IssueScope.Organization
                : IssueScope.Mine;

            var issues = _portalManager.GetIssues(scope).AsEnumerable();

            var q = request?.GetParameter("q")?.Value;
            if (!string.IsNullOrWhiteSpace(q))
            {
                issues = issues.Where(i =>
                    (i.Key ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (i.Title ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            var status = request?.GetParameter("status")?.Value;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PortalIssueState>(status, true, out var state))
            {
                issues = issues.Where(i => i.PortalState == state);
            }

            var payload = issues
                .Select(i => PortalApi.ToDto(i, includeTimeline: false))
                .ToList();

            return PortalApi.Json(payload);
        }

        /// <summary>
        /// Handles <c>POST {base}</c>: creates a new issue against the supplied request
        /// type and optional template.
        /// </summary>
        /// <param name="request">The incoming request carrying the JSON body.</param>
        /// <returns>
        /// <c>201 Created</c> with the issue projection, <c>400</c> on validation
        /// errors, or <c>404</c> when the request type does not exist.
        /// </returns>
        [Method(RequestMethod.POST)]
        public IResponse Create(IRequest request)
        {
            if (!PortalApi.TryReadBody<CreateIssuePayload>(request, out var payload))
            {
                return PortalApi.Error("The request body must be a JSON document with at least 'requestTypeKey' and 'title'.");
            }

            if (string.IsNullOrWhiteSpace(payload.RequestTypeKey) || string.IsNullOrWhiteSpace(payload.Title))
            {
                return PortalApi.Error("'requestTypeKey' and 'title' are required.");
            }

            try
            {
                var issue = _portalManager.CreateIssue(
                    payload.RequestTypeKey,
                    payload.TemplateKey,
                    payload.Title,
                    payload.Description,
                    payload.Priority);

                return PortalApi.Created(PortalApi.ToDto(issue, includeTimeline: true));
            }
            catch (InvalidOperationException)
            {
                // the manager reports an unknown request type this way
                return PortalApi.NotFound();
            }
        }

        /// <summary>
        /// The JSON body accepted by <c>POST</c>.
        /// </summary>
        private sealed class CreateIssuePayload
        {
            /// <summary>Gets or sets the request type to submit against.</summary>
            public string RequestTypeKey { get; set; }

            /// <summary>Gets or sets the template to use, or <see langword="null"/>.</summary>
            public string TemplateKey { get; set; }

            /// <summary>Gets or sets the short description of the issue.</summary>
            public string Title { get; set; }

            /// <summary>Gets or sets the long description.</summary>
            public string Description { get; set; }

            /// <summary>Gets or sets the priority code (<c>P1</c>–<c>P4</c>).</summary>
            public string Priority { get; set; }
        }
    }
}
