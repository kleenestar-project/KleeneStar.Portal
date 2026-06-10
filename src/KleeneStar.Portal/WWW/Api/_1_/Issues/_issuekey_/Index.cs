using KleeneStar.Portal.WebAttribute;
using KleeneStar.Portal.WebManager;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Issues._issuekey_
{
    /// <summary>
    /// REST endpoint over a single portal issue. The URL is
    /// <c>/api/1/issues/{issuekey}</c> below the portal application; the
    /// <c>{issuekey}</c> URL segment is declared via
    /// <see cref="IssueKeySegmentAttribute"/> and binds the sibling action endpoints
    /// (<c>comments</c>, <c>share</c>, <c>watch</c>, <c>accept</c>, <c>reject</c>) to
    /// the same path variable.
    /// </summary>
    [Title("kleenestar.portal:api.issue.title")]
    [IssueKeySegment]
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
        /// Handles <c>GET {base}</c>: returns the full issue projection — metadata,
        /// participants, description, and the comment timeline.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The issue JSON, or <c>404</c> when the issue is not visible.</returns>
        [Method(RequestMethod.GET)]
        public IResponse Retrieve(IRequest request)
        {
            var issue = _portalManager.GetIssue(PortalApi.GetIssueKey(request));

            return issue is null
                ? PortalApi.NotFound()
                : PortalApi.Json(PortalApi.ToDto(issue, includeTimeline: true));
        }
    }
}
