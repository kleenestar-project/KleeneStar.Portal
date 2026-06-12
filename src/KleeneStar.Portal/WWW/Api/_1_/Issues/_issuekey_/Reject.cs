using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebManager;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Issues._issuekey_
{
    /// <summary>
    /// REST endpoint rejecting a proposed resolution. The URL is
    /// <c>/api/1/issues/{issuekey}/reject</c> below the portal application; the
    /// <c>{issuekey}</c> segment is declared by the sibling <see cref="Index"/>
    /// endpoint, so this class must NOT carry the segment attribute itself.
    /// </summary>
    [Title("kleenestar.portal:api.issue.reject.title")]
    [Cache]
    public sealed class Reject : IRestApi
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
        public Reject()
        {
        }

        /// <summary>
        /// Handles <c>POST {base}</c>: rejects the proposed resolution with a mandatory
        /// reason and returns the issue to <see cref="PortalIssueState.InProgress"/>.
        /// </summary>
        /// <param name="request">The incoming request carrying the JSON body.</param>
        /// <returns>
        /// <c>204 No Content</c> on success, <c>400</c> when the reason is missing, a
        /// state conflict when no resolution is pending, or <c>404</c> when the issue
        /// is not visible.
        /// </returns>
        [Method(RequestMethod.POST)]
        public IResponse Create(IRequest request)
        {
            if (!PortalApi.TryReadBody<RejectPayload>(request, out var payload)
                || string.IsNullOrWhiteSpace(payload.Reason))
            {
                return PortalApi.Error("'reason' is required.");
            }

            var key = PortalApi.GetIssueKey(request);
            var issue = _portalManager.GetIssue(key);

            if (issue is null)
            {
                return PortalApi.NotFound();
            }

            return PortalApi.RunWithConflictMapping(() =>
            {
                _portalManager.RejectResolution(key, payload.Reason);
                return PortalApi.NoContent();
            });
        }

        /// <summary>
        /// The JSON body accepted by <c>POST</c>.
        /// </summary>
        private sealed class RejectPayload
        {
            /// <summary>Gets or sets the mandatory rejection reason.</summary>
            public string Reason { get; set; }
        }
    }
}
