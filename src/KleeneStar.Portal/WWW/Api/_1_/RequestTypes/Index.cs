using KleeneStar.Portal.WebManager;
using System.Linq;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.RequestTypes
{
    /// <summary>
    /// REST endpoint listing the request-type catalog of the portal. The URL is
    /// <c>/api/1/requesttypes</c> below the portal application; the payload matches
    /// the "request types" table of <c>kleenestar.portal.md</c>, including the
    /// templates of each request type.
    /// </summary>
    [Title("kleenestar.portal:api.requesttypes.title")]
    [Cache]
    public sealed class Index : IRestApi
    {
        /// <summary>
        /// Gets the portal manager resolved through the <see cref="PortalHub"/> facade.
        /// REST endpoints are constructed by the framework without dependency
        /// injection, so the manager is reached the same way the existing core
        /// endpoints reach <c>CoreHub</c>.
        /// </summary>
        private static IPortalManager _portalManager => PortalHub.PortalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Index()
        {
        }

        /// <summary>
        /// Handles <c>GET {base}</c>: returns the request types visible to the calling
        /// identity, including their templates.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The catalog as a JSON array.</returns>
        [Method(RequestMethod.GET)]
        public IResponse Retrieve(IRequest request)
        {
            var catalog = _portalManager.GetRequestTypes()
                .Select(PortalApi.ToDto)
                .ToList();

            return PortalApi.Json(catalog);
        }
    }
}
