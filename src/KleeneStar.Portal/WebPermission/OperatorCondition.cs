using KleeneStar.Core;
using System;
using WebExpress.WebCore.WebCondition;
using WebExpress.WebCore.WebMessage;

namespace KleeneStar.Portal.WebPermission
{
    /// <summary>
    /// Holds when the caller is an operator-side account: signed in and belonging to no tenant.
    /// </summary>
    /// <remarks>
    /// The portal serves two audiences in one shell. A customer (an account with a tenant) is
    /// offered the request catalog and their issues; the portal administration is offered to the
    /// service side only, and its pages refuse everybody else anyway
    /// (<see cref="PortalAdminAuthorization"/>) - the condition keeps the entry from being shown
    /// to someone it would refuse.
    /// </remarks>
    public sealed class OperatorCondition : ICondition
    {
        /// <summary>
        /// Determines whether the caller is an operator-side account.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><see langword="true"/> for a signed-in account without a tenant.</returns>
        public bool Fulfillment(IRequest request)
        {
            var identityId = CoreHub.SessionManager?.GetCurrentIdentityId(request) ?? Guid.Empty;

            return identityId != Guid.Empty
                && CoreHub.IdentityManager?.GetIdentity(identityId) is { TenantId: null };
        }
    }
}
