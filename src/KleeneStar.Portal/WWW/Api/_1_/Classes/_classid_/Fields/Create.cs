using KleeneStar.Core.WebParameter;
using KleeneStar.Core.WebAttribute;
using KleeneStar.Model.Entities;
using KleeneStar.Portal.WebManager;
using System;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Fields
{
    /// <summary>
    /// REST endpoint that creates a new field on the given class. Mirrors the
    /// <c>POST /api/1/portal/classes/{classid}/fields</c> URL; the body carries
    /// the editable properties (name, description, fieldType, cardinality,
    /// required, unique).
    /// </summary>
    [Title("kleenestar.portal:api.admin.field.create.title")]
    [ClassIdSegment]
    [Cache]
    public sealed class Create : IRestApi
    {
        private static IPortalManager _portalManager => PortalHub.PortalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Create()
        {
        }

        /// <summary>
        /// Handles <c>POST /api/1/portal/classes/{classid}/fields</c>.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>201 Created with the new field, or 400/404 on validation errors.</returns>
        [Method(RequestMethod.POST)]
        public IResponse CreateField(IRequest request)
        {
            var classIdParameter = request?.GetParameter<ClassIdParameter>();
            if (classIdParameter is null || !Guid.TryParse(classIdParameter.Value, out var classId))
            {
                return PortalAdminApi.BadRequest("Invalid class id.");
            }

            if (!PortalAdminApi.TryReadBody<FieldPayload>(request, out var payload)
                || string.IsNullOrWhiteSpace(payload.Name))
            {
                return PortalAdminApi.BadRequest("'name' is required.");
            }

            try
            {
                var field = _portalManager.AddField(
                    classId,
                    payload.Name,
                    payload.Description,
                    payload.FieldType ?? FieldType.Text,
                    payload.Cardinality ?? FieldCardinality.Single,
                    payload.Required ?? false,
                    payload.Unique ?? false);

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
