using KleeneStar.Core.WebAttribute;
using KleeneStar.Core.WebParameter;
using KleeneStar.Portal.WebManager;
using System;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Classes._classid_
{
    /// <summary>
    /// REST endpoint flipping the <c>PortalVisible</c> flag of a class. The
    /// toggle is a <c>POST</c> with no body — the server derives the new state
    /// from the current one.
    /// </summary>
    [Title("kleenestar.portal:api.admin.class.portalvisible.title")]
    [ClassIdSegment]
    [Cache]
    public sealed class PortalVisible : IRestApi
    {
        private static IPortalManager _portalManager => PortalHub.PortalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public PortalVisible()
        {
        }

        /// <summary>
        /// Handles <c>POST /api/1/portal/classes/{classid}/portalvisible</c>.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The updated class, or 404 when the class is unknown.</returns>
        [Method(RequestMethod.POST)]
        public IResponse Toggle(IRequest request)
        {
            var classIdParameter = request?.GetParameter<ClassIdParameter>();
            if (classIdParameter is null || !Guid.TryParse(classIdParameter.Value, out var classId))
            {
                return PortalAdminApi.BadRequest("Invalid class id.");
            }

            var cls = _portalManager.TogglePortalVisible(classId);
            if (cls is null)
            {
                return PortalAdminApi.NotFound();
            }

            return PortalAdminApi.Json(new
            {
                id = cls.Id,
                portalVisible = cls.PortalVisible
            });
        }
    }
}
