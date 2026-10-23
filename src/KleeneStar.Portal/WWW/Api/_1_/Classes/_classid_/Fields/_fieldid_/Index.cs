using KleeneStar.Core;
using KleeneStar.Core.WebAttribute;
using KleeneStar.Core.WebParameter;
using System;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Fields._fieldid_
{
    /// <summary>
    /// REST endpoint over a single field of the portal administration. The URL is
    /// <c>/api/1/classes/{classid}/fields/{fieldid}</c> below the portal application; it binds
    /// the <c>{fieldid}</c> segment for the clone, delete and update endpoints beside it.
    /// </summary>
    [Title("kleenestar.portal:api.admin.field.title")]
    [FieldIdSegment]
    [Cache]
    public sealed class Index : IRestApi
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Index()
        {
        }

        /// <summary>
        /// Handles <c>GET {base}</c>: returns the field.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The field, <c>403</c> for a caller who may not administer its class, or <c>404</c>.</returns>
        [Method(RequestMethod.GET)]
        public IResponse Retrieve(IRequest request)
        {
            var fieldIdParameter = request?.GetParameter<FieldIdParameter>();
            if (fieldIdParameter is null || !Guid.TryParse(fieldIdParameter.Value, out var fieldId))
            {
                return PortalAdminApi.BadRequest("Invalid field id.");
            }

            var field = CoreHub.FieldManager.GetField(fieldId);
            if (field is null)
            {
                return PortalAdminApi.NotFound();
            }

            if (!PortalAdminApi.MayAdminister(field.ClassId, request))
            {
                return PortalAdminApi.Forbidden();
            }

            return PortalAdminApi.Json(new
            {
                id = field.Id,
                name = field.Name,
                type = field.FieldType.ToString(),
                required = field.Required,
                deprecated = field.Deprecated
            });
        }
    }
}
