using KleeneStar.Core.WebParameter;
using KleeneStar.Core.WebAttribute;
using KleeneStar.Portal.WebManager;
using System;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Fields._fieldid_
{
    /// <summary>
    /// REST endpoint that soft-deletes (deprecates) a field. The underlying row
    /// is preserved for audit; the field is hidden from portal list projections.
    /// </summary>
    [Title("kleenestar.portal:api.admin.field.delete.title")]
    [FieldIdSegment]
    [Cache]
    public sealed class Delete : IRestApi
    {
        private static IPortalManager _portalManager => PortalHub.PortalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Delete()
        {
        }

        /// <summary>
        /// Handles <c>DELETE /api/1/portal/classes/{classid}/fields/{fieldid}</c>.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>204 No Content on success, or 404 when the field is unknown.</returns>
        [Method(RequestMethod.DELETE)]
        public IResponse DeleteField(IRequest request)
        {
            var fieldIdParameter = request?.GetParameter<FieldIdParameter>();
            if (fieldIdParameter is null || !Guid.TryParse(fieldIdParameter.Value, out var fieldId))
            {
                return PortalAdminApi.BadRequest("Invalid field id.");
            }

            var ok = _portalManager.DeleteField(fieldId);
            if (!ok)
            {
                return PortalAdminApi.NotFound();
            }

            return PortalAdminApi.NoContent();
        }
    }
}
