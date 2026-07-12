using KleeneStar.Portal.WebManager;
using System.Collections.Generic;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Issues._issuekey_
{
    /// <summary>
    /// REST endpoint over an issue's shared-with list. The URL is
    /// <c>/api/1/issues/{issuekey}/share</c> below the portal application; the
    /// <c>{issuekey}</c> segment is declared by the sibling <see cref="Index"/>
    /// endpoint, so this class must NOT carry the segment attribute itself.
    /// </summary>
    /// <remarks>
    /// <c>POST</c> adds one or more identities of the tenant
    /// (body: <c>{ "identities": ["&lt;id&gt;", …] }</c>). <c>DELETE ?identity=&lt;id&gt;</c>
    /// revokes a single share — the concept document routes the revocation as
    /// <c>share/{id}</c>; the identity travels as a query parameter here because the
    /// path variable is already taken by the issue key.
    /// </remarks>
    [Title("kleenestar.portal:api.issue.share.title")]
    [Cache]
    public sealed class Share : IRestApi
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
        public Share()
        {
        }

        /// <summary>
        /// Handles <c>POST {base}</c>: adds the supplied identities to the issue's
        /// shared-with list.
        /// </summary>
        /// <param name="request">The incoming request carrying the JSON body.</param>
        /// <returns>
        /// <c>204 No Content</c> on success, <c>400</c> when no identities were
        /// supplied, or <c>404</c> when the issue is not visible.
        /// </returns>
        [Method(RequestMethod.POST)]
        public IResponse Create(IRequest request)
        {
            if (!PortalApi.TryReadBody<SharePayload>(request, out var payload)
                || payload.Identities is not { Count: > 0 })
            {
                return PortalApi.Error("'identities' must contain at least one identity id.");
            }

            var issue = _portalManager.ShareIssue(PortalApi.GetIssueKey(request), payload.Identities);

            return issue is null ? PortalApi.NotFound() : PortalApi.NoContent();
        }

        /// <summary>
        /// Handles <c>DELETE {base}?identity=…</c>: revokes the share of the supplied
        /// identity.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>
        /// <c>204 No Content</c> on success, <c>400</c> when the identity parameter is
        /// missing, or <c>404</c> when the issue is not visible.
        /// </returns>
        [Method(RequestMethod.DELETE)]
        public IResponse Delete(IRequest request)
        {
            var identity = request?.GetParameter("identity")?.Value;

            if (string.IsNullOrWhiteSpace(identity))
            {
                return PortalApi.Error("The 'identity' query parameter is required.");
            }

            var issue = _portalManager.UnshareIssue(PortalApi.GetIssueKey(request), identity);

            return issue is null ? PortalApi.NotFound() : PortalApi.NoContent();
        }

        /// <summary>
        /// The JSON body accepted by <c>POST</c>.
        /// </summary>
        private sealed class SharePayload
        {
            /// <summary>Gets or sets the identity ids to add.</summary>
            public List<string> Identities { get; set; }
        }
    }
}
