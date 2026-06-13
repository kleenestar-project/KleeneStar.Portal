using KleeneStar.Core.WebParameter;
using KleeneStar.Core.WebAttribute;
using KleeneStar.Portal.WebManager;
using System;
using System.Linq;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Forms
{
    /// <summary>
    /// REST endpoint listing the active forms of a class. The
    /// <c>portalTemplate</c> flag identifies forms that surface as
    /// service-request templates.
    /// </summary>
    [Title("kleenestar.portal:api.admin.forms.title")]
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
        /// Handles <c>GET /api/1/portal/classes/{classid}/forms</c>.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The ordered form list, or 404 when the class is unknown.</returns>
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

            var payload = _portalManager.GetForms(classId)
                .Select(f => new
                {
                    id = f.Id,
                    name = f.Name,
                    description = f.Description ?? string.Empty,
                    portalTemplate = f.PortalTemplate
                })
                .ToList();

            return PortalAdminApi.Json(payload);
        }
    }
}
