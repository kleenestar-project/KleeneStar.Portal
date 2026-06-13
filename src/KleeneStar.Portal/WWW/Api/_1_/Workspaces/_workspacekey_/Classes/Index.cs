using KleeneStar.Core.WebParameter;
using KleeneStar.Core.WebAttribute;
using KleeneStar.Portal.WebManager;
using System.Linq;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Workspaces._workspacekey_.Classes
{
    /// <summary>
    /// REST endpoint listing the classes of a workspace. Mirrors the operator-side
    /// class projection. The <c>{workspacekey}</c> URL segment is bound by
    /// <see cref="WorkspaceKeySegmentAttribute"/> and resolved through
    /// <see cref="WorkspaceKeyParameter"/>.
    /// </summary>
    [Title("kleenestar.portal:api.admin.classes.title")]
    [WorkspaceKeySegment]
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
        /// Handles <c>GET /api/1/portal/workspaces/{workspacekey}/classes</c>.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The ordered class list, or 404 when the workspace is unknown.</returns>
        [Method(RequestMethod.GET)]
        public IResponse Retrieve(IRequest request)
        {
            var keyParameter = request.GetParameter<WorkspaceKeyParameter>();
            var workspace = _portalManager.GetWorkspace(keyParameter?.Value);
            if (workspace is null)
            {
                return PortalAdminApi.NotFound();
            }

            var payload = _portalManager.GetClasses(workspace.Id)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    description = c.Description ?? string.Empty,
                    portalVisible = c.PortalVisible,
                    state = c.State
                })
                .ToList();

            return PortalAdminApi.Json(payload);
        }
    }
}
