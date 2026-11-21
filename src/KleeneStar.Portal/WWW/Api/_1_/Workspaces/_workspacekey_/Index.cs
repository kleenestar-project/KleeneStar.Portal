using KleeneStar.Core.WebAttribute;
using KleeneStar.Core.WebParameter;
using KleeneStar.Portal.WebManager;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Workspaces._workspacekey_
{
    /// <summary>
    /// REST endpoint over a single workspace of the portal administration. The URL is
    /// <c>/api/1/workspaces/{workspacekey}</c> below the portal application; it binds the
    /// <c>{workspacekey}</c> segment for the class list beside it.
    /// </summary>
    [Title("kleenestar.portal:api.admin.workspace.title")]
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
        /// Handles <c>GET {base}</c>: returns the workspace.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The workspace, <c>403</c> for a caller who may not administer it, or <c>404</c>.</returns>
        [Method(RequestMethod.GET)]
        public IResponse Retrieve(IRequest request)
        {
            var workspace = _portalManager.GetWorkspace(request?.GetParameter<WorkspaceKeyParameter>()?.Value);
            if (workspace is null)
            {
                return PortalAdminApi.NotFound();
            }

            if (!KleeneStar.Core.WebRestApi.ContentAuthorization.MayAdministerWorkspace(workspace.Id, request))
            {
                return PortalAdminApi.Forbidden();
            }

            return PortalAdminApi.Json(new
            {
                id = workspace.Id,
                key = workspace.Key,
                name = workspace.Name,
                state = workspace.State
            });
        }
    }
}
