using KleeneStar.Core.WebParameter;
using KleeneStar.Core.WebAttribute;
using KleeneStar.Model.Entities;
using KleeneStar.Portal.WebManager;
using System;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Fields._fieldid_
{
    /// <summary>
    /// REST endpoint updating a field. The <c>{fieldid}</c> URL segment is bound
    /// by <see cref="FieldIdSegmentAttribute"/>; the body carries the editable
    /// properties.
    /// </summary>
    [Title("kleenestar.portal:api.admin.field.update.title")]
    [FieldIdSegment]
    [Cache]
    public sealed class Update : IRestApi
    {
        private static IPortalManager _portalManager => PortalHub.PortalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Update()
        {
        }

        /// <summary>
        /// Handles <c>PUT /api/1/portal/classes/{classid}/fields/{fieldid}</c>.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>200 OK with the updated field, or 400/404 on validation errors.</returns>
        [Method(RequestMethod.PUT)]
        public IResponse UpdateField(IRequest request)
        {
            var fieldIdParameter = request?.GetParameter<FieldIdParameter>();
            if (fieldIdParameter is null || !Guid.TryParse(fieldIdParameter.Value, out var fieldId))
            {
                return PortalAdminApi.BadRequest("Invalid field id.");
            }

            if (!PortalAdminApi.TryReadBody<FieldPayload>(request, out var payload)
                || string.IsNullOrWhiteSpace(payload.Name))
            {
                return PortalAdminApi.BadRequest("'name' is required.");
            }

            try
            {
                var field = _portalManager.UpdateField(
                    fieldId,
                    payload.Name,
                    payload.Description,
                    payload.FieldType ?? FieldType.Text,
                    payload.Cardinality ?? FieldCardinality.Single,
                    payload.Required ?? false,
                    payload.Unique ?? false);

                if (field is null)
                {
                    return PortalAdminApi.NotFound();
                }

                return PortalAdminApi.Json(new
                {
                    id = field.Id,
                    name = field.Name,
                    fieldType = field.FieldType,
                    cardinality = field.Cardinality
                });
            }
            catch (InvalidOperationException ex)
            {
                return PortalAdminApi.BadRequest(ex.Message);
            }
        }

        private sealed class FieldPayload
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public FieldType? FieldType { get; set; }
            public FieldCardinality? Cardinality { get; set; }
            public bool? Required { get; set; }
            public bool? Unique { get; set; }
        }
    }
}
