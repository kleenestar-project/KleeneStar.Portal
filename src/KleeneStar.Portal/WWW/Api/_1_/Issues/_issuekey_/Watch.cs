using KleeneStar.Portal.WebManager;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Issues._issuekey_
{
    /// <summary>
    /// REST endpoint over the calling identity's watch subscription on an issue. The
    /// URL is <c>/api/1/issues/{issuekey}/watch</c> below the portal application; the
    /// <c>{issuekey}</c> segment is declared by the sibling <see cref="Index"/>
    /// endpoint, so this class must NOT carry the segment attribute itself.
    /// </summary>
    [Title("kleenestar.portal:api.issue.watch.title")]
    [Cache]
    public sealed class Watch : IRestApi
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
        public Watch()
        {
        }

        /// <summary>
        /// Handles <c>POST {base}</c>: subscribes the calling identity to the issue's
        /// notifications.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns><c>204 No Content</c>, or <c>404</c> when the issue is not visible.</returns>
        [Method(RequestMethod.POST)]
        public IResponse Create(IRequest request)
        {
            var issue = _portalManager.Watch(PortalApi.GetIssueKey(request));

            return issue is null ? PortalApi.NotFound() : PortalApi.NoContent();
        }

        /// <summary>
        /// Handles <c>DELETE {base}</c>: unsubscribes the calling identity.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns><c>204 No Content</c>, or <c>404</c> when the issue is not visible.</returns>
        [Method(RequestMethod.DELETE)]
        public IResponse Delete(IRequest request)
        {
            return PortalApi.RunWithConflictMapping(() =>
            {
                var issue = _portalManager.Unwatch(PortalApi.GetIssueKey(request));
                return issue is null ? PortalApi.NotFound() : PortalApi.NoContent();
            });
        }
    }
}
