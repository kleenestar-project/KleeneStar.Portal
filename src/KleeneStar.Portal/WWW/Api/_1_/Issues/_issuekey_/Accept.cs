using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebManager;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Issues._issuekey_
{
    /// <summary>
    /// REST endpoint accepting a proposed resolution. The URL is
    /// <c>/api/1/issues/{issuekey}/accept</c> below the portal application; the
    /// <c>{issuekey}</c> segment is declared by the sibling <see cref="Index"/>
    /// endpoint, so this class must NOT carry the segment attribute itself.
    /// </summary>
    /// <remarks>
    /// Accepting is idempotent on closed issues (<c>204</c> with no further effect);
    /// accepting an issue whose resolution has never been proposed is a
    /// <c>409 Conflict</c> (see <see cref="PortalApi.Conflict"/>).
    /// </remarks>
    [Title("kleenestar.portal:api.issue.accept.title")]
    [Cache]
    public sealed class Accept : IRestApi
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
        public Accept()
        {
        }

        /// <summary>
        /// Handles <c>POST {base}</c>: accepts the proposed resolution and closes the
        /// issue.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>
        /// <c>204 No Content</c> on success or on an already closed issue, a state
        /// conflict when no resolution is pending, or <c>404</c> when the issue is not
        /// visible.
        /// </returns>
        [Method(RequestMethod.POST)]
        public IResponse Create(IRequest request)
        {
            var key = PortalApi.GetIssueKey(request);
            var issue = _portalManager.GetIssue(key);

            if (issue is null)
            {
                return PortalApi.NotFound();
            }

            // closed issues are an idempotent no-op (concept §API). Every other
            // non-Resolved state — including stale Idempotency — is a 409 surfaced
            // by the manager as a PortalConflictException.
            if (issue.PortalState == PortalIssueState.Closed)
            {
                return PortalApi.NoContent();
            }

            return PortalApi.RunWithConflictMapping(() =>
            {
                _portalManager.AcceptResolution(key);
                return PortalApi.NoContent();
            });
        }
    }
}
