using KleeneStar.Core;
using KleeneStar.Core.WebAttribute;
using KleeneStar.Core.WebParameter;
using KleeneStar.Model.Entities;
using System;
using System.Linq;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;
using WebExpress.WebIndex.Queries;

namespace KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Forms._formid_
{
    /// <summary>
    /// REST endpoint over a single form of the portal administration. The URL is
    /// <c>/api/1/classes/{classid}/forms/{formid}</c> below the portal application; it binds
    /// the <c>{formid}</c> segment for the template toggle beside it.
    /// </summary>
    [Title("kleenestar.portal:api.admin.form.title")]
    [FormIdSegment]
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
        /// Handles <c>GET {base}</c>: returns the form.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The form, <c>403</c> for a caller who may not administer its class, or <c>404</c>.</returns>
        [Method(RequestMethod.GET)]
        public IResponse Retrieve(IRequest request)
        {
            var formIdParameter = request?.GetParameter<FormIdParameter>();
            if (formIdParameter is null || !Guid.TryParse(formIdParameter.Value, out var formId))
            {
                return PortalAdminApi.BadRequest("Invalid form id.");
            }

            var form = CoreHub.FormManager
                .GetForms(new Query<Form>())
                .FirstOrDefault(f => f.Id == formId);
            if (form is null)
            {
                return PortalAdminApi.NotFound();
            }

            if (!PortalAdminApi.MayAdminister(form.ClassId, request))
            {
                return PortalAdminApi.Forbidden();
            }

            return PortalAdminApi.Json(new
            {
                id = form.Id,
                name = form.Name,
                portalTemplate = form.PortalTemplate
            });
        }
    }
}
