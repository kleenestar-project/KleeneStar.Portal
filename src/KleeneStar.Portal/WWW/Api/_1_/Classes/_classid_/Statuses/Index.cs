using KleeneStar.Core.WebParameter;
using KleeneStar.Core.WebAttribute;
using KleeneStar.Portal.WebManager;
using System;
using System.Linq;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Statuses
{
    /// <summary>
    /// REST endpoint listing the active statuses of a class.
    /// </summary>
    [Title("kleenestar.portal:api.admin.statuses.title")]
    [ClassIdSegment]
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
        /// Handles <c>GET /api/1/portal/classes/{classid}/statuses</c>.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The ordered status list, or 404 when the class is unknown.</returns>
        [Method(RequestMethod.GET)]
        public IResponse Retrieve(IRequest request)
        {
            var classIdParameter = request?.GetParameter<ClassIdParameter>();
            if (classIdParameter is null || !Guid.TryParse(classIdParameter.Value, out var classId))
            {
                return PortalAdminApi.BadRequest("Invalid class id.");
            }

            if (_portalManager.GetClass(classId) is null)
            {
                return PortalAdminApi.NotFound();
            }

            var payload = _portalManager.GetStatuses(classId)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    description = s.Description ?? string.Empty
                })
                .ToList();

            return PortalAdminApi.Json(payload);
        }
    }
}
