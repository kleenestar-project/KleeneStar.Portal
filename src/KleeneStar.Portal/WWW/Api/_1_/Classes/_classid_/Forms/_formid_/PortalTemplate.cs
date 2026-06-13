using KleeneStar.Core.WebParameter;
using KleeneStar.Core.WebAttribute;
using KleeneStar.Portal.WebManager;
using System;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Forms._formid_
{
    /// <summary>
    /// REST endpoint that flips the <c>PortalTemplate</c> flag of a form. The
    /// <c>{formid}</c> URL segment is bound by
    /// <see cref="FormIdSegmentAttribute"/>.
    /// </summary>
    [Title("kleenestar.portal:api.admin.form.portaltemplate.title")]
    [FormIdSegment]
    [Cache]
    public sealed class PortalTemplate : IRestApi
    {
        private static IPortalManager _portalManager => PortalHub.PortalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public PortalTemplate()
        {
        }

        /// <summary>
        /// Handles <c>POST /api/1/portal/classes/{classid}/forms/{formid}/portaltemplate</c>.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The updated form, or 404 when the form is unknown.</returns>
        [Method(RequestMethod.POST)]
        public IResponse Toggle(IRequest request)
        {
            var formIdParameter = request?.GetParameter<FormIdParameter>();
            if (formIdParameter is null || !Guid.TryParse(formIdParameter.Value, out var formId))
            {
                return PortalAdminApi.BadRequest("Invalid form id.");
            }

            var form = _portalManager.TogglePortalTemplate(formId);
            if (form is null)
            {
                return PortalAdminApi.NotFound();
            }

            return PortalAdminApi.Json(new
            {
                id = form.Id,
                portalTemplate = form.PortalTemplate
            });
        }
    }
}
