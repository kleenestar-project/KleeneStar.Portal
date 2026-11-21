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
    /// REST endpoint over a single class of the portal administration. The URL is
    /// <c>/api/1/classes/{classid}</c> below the portal application.
    /// </summary>
    /// <remarks>
    /// The endpoint is what binds the <c>{classid}</c> segment for the class's sibling
    /// endpoints (fields, forms, priorities, statuses, the publish toggle): a segment attribute
    /// belongs on the <c>Index</c> of its folder only. The siblings used to carry it themselves,
    /// which mapped every one of them onto <c>/classes/_classid_/{classid}/{classid}</c>.
    /// </remarks>
    [Title("kleenestar.portal:api.admin.class.title")]
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
        /// Handles <c>GET /api/1/classes/{classid}</c>: returns the class as the portal
        /// administration shows it.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The class, <c>403</c> for a caller who may not administer it, or <c>404</c>.</returns>
        [Method(RequestMethod.GET)]
        public IResponse Retrieve(IRequest request)
        {
            var classIdParameter = request?.GetParameter<ClassIdParameter>();
            if (classIdParameter is null || !Guid.TryParse(classIdParameter.Value, out var classId))
            {
                return PortalAdminApi.BadRequest("Invalid class id.");
            }

            var cls = _portalManager.GetClass(classId);
            if (cls is null)
            {
                return PortalAdminApi.NotFound();
            }

            if (!PortalAdminApi.MayAdminister(classId, request))
            {
                return PortalAdminApi.Forbidden();
            }

            return PortalAdminApi.Json(new
            {
                id = cls.Id,
                name = cls.Name,
                portalVisible = cls.PortalVisible
            });
        }
    }
}
