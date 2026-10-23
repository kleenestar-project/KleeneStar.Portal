using KleeneStar.Core;
using KleeneStar.Core.WebParameter;
using KleeneStar.Core.WebPermission;
using KleeneStar.Core.WebPermissions;
using System;
using WebExpress.WebCore.WebMessage;

namespace KleeneStar.Portal.WebPermission
{
    /// <summary>
    /// The guard of the portal's administration pages - the workspaces, their classes and the
    /// fields, forms, priorities and statuses a class is published with.
    /// </summary>
    /// <remarks>
    /// The administration is the operator side's class administration under the portal's roof,
    /// so it demands what the operator side demands on the chain the route names: administering
    /// the class (<see cref="ClassUpdatePermission"/>) where a class is addressed, the workspace
    /// (<see cref="WorkspaceUpdatePermission"/>) where only a workspace is. The overview of all
    /// workspaces names neither and is for operator-side accounts: a signed-in caller without a
    /// tenant. A refusal is <c>ForbiddenException</c>, answered in place - the sign-in prompt
    /// for an anonymous caller, the access-denied page otherwise. The pages used to render for
    /// anybody, and their endpoints changed the class structure for anybody.
    /// </remarks>
    public static class PortalAdminAuthorization
    {
        /// <summary>
        /// Refuses the request unless its caller may administer what its route addresses.
        /// </summary>
        /// <param name="request">The request of the administration page.</param>
        /// <exception cref="WebExpress.WebCore.WebMessage.ForbiddenException">The caller is refused.</exception>
        public static void Demand(IRequest request)
        {
            var classKey = request?.GetParameter<ClassIdParameter>()?.Value;

            if (Guid.TryParse(classKey, out var classId))
            {
                var @class = CoreHub.ClassManager.GetClass(classId);

                if (@class is not null)
                {
                    PageAuthorization.Demand(request, typeof(ClassUpdatePermission), PageAuthorization.ChainOf(@class));
                }

                return;
            }

            var workspaceKey = request?.GetParameter<WorkspaceKeyParameter>()?.Value;

            if (!string.IsNullOrWhiteSpace(workspaceKey))
            {
                var workspace = CoreHub.WorkspaceManager.GetWorkspaceByKey(workspaceKey);

                if (workspace is not null)
                {
                    PageAuthorization.Demand(request, typeof(WorkspaceUpdatePermission), PageAuthorization.ChainOf(workspace));
                }

                return;
            }

            var identityId = CoreHub.SessionManager.GetCurrentIdentityId(request);
            var identity = identityId == Guid.Empty ? null : CoreHub.IdentityManager.GetIdentity(identityId);

            if (identity is null || identity.TenantId is not null)
            {
                throw PageAuthorization.Refuse();
            }
        }
    }
}
