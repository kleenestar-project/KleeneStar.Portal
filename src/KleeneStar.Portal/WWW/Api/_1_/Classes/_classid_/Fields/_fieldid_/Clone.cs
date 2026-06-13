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
    /// REST endpoint cloning a field with a unique new name. The source field
    /// stays active; the clone is created in the same class and starts active
    /// as well.
    /// </summary>
    [Title("kleenestar.portal:api.admin.field.clone.title")]
    [FieldIdSegment]
    [Cache]
    public sealed class Clone : IRestApi
    {
        private static IPortalManager _portalManager => PortalHub.PortalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Clone()
        {
        }

        /// <summary>
        /// Handles <c>POST /api/1/portal/classes/{classid}/fields/{fieldid}/clone</c>.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>201 Created with the clone, or 404 when the source is unknown.</returns>
        [Method(RequestMethod.POST)]
        public IResponse CloneField(IRequest request)
        {
            var fieldIdParameter = request?.GetParameter<FieldIdParameter>();
            if (fieldIdParameter is null || !Guid.TryParse(fieldIdParameter.Value, out var fieldId))
            {
                return PortalAdminApi.BadRequest("Invalid field id.");
            }

            var clone = _portalManager.CloneField(fieldId);
            if (clone is null)
            {
                return PortalAdminApi.NotFound();
            }

            return PortalAdminApi.Json(new
            {
                id = clone.Id,
                name = clone.Name
            });
        }
    }
}
