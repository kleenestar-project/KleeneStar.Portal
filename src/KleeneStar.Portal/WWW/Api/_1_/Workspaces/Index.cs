using KleeneStar.Portal.WebManager;
using System.Linq;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Workspaces
{
    /// <summary>
    /// REST endpoint listing the workspaces visible to the calling identity in
    /// the customer portal. Mirrors the operator-side workspace projection with
    /// tenant isolation applied (see <see cref="IPortalManager.GetWorkspaces"/>).
    /// </summary>
    [Title("kleenestar.portal:api.admin.workspaces.title")]
    [Cache]
    public sealed class Index : IRestApi
    {
        private static IPortalManager _portalManager => PortalHub.PortalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Index()
        {
        }

        /// <summary>
        /// Handles <c>GET /api/1/portal/workspaces</c>: returns the ordered
        /// workspace list as a JSON array.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The workspace list.</returns>
        [Method(RequestMethod.GET)]
        public IResponse Retrieve(IRequest request)
        {
            var payload = _portalManager.GetWorkspaces()
                .Select(w => new
                {
                    id = w.Id,
                    key = w.Key,
                    name = w.Name,
                    description = w.Description ?? string.Empty,
                    state = w.State
                })
                .ToList();

            return PortalAdminApi.Json(payload);
        }
    }
}
