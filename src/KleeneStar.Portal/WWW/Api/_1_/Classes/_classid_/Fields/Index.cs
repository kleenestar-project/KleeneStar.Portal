using KleeneStar.Core.WebParameter;
using KleeneStar.Core.WebAttribute;
using KleeneStar.Portal.WebManager;
using System;
using System.Linq;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Fields
{
    /// <summary>
    /// REST endpoint listing the fields of a class. Mirrors the operator-side
    /// field projection with deprecated fields filtered out.
    /// </summary>
    [Title("kleenestar.portal:api.admin.fields.title")]
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
        /// Handles <c>GET /api/1/portal/classes/{classid}/fields</c>.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The ordered field list, or 404 when the class is unknown.</returns>
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

            var payload = _portalManager.GetFields(classId)
                .Select(f => new
                {
                    id = f.Id,
                    name = f.Name,
                    description = f.Description ?? string.Empty,
                    fieldType = f.FieldType,
                    cardinality = f.Cardinality,
                    required = f.Required,
                    unique = f.Unique
                })
                .ToList();

            return PortalAdminApi.Json(payload);
        }
    }
}
