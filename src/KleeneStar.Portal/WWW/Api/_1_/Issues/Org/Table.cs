using KleeneStar.Portal.WebDomain;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Issues.Org
{
    /// <summary>
    /// REST table endpoint backing the "Organization" view. Returns the issues of the
    /// calling identity's tenant (scope <c>Organization</c>) in the
    /// <c>RestApiTableResult</c> shape the <c>ControlRestTable</c> client consumes. The
    /// filtering, paging, and shaping is shared with the "My Issues" table via
    /// <see cref="IssueTableProjection"/>.
    /// </summary>
    /// <remarks>
    /// The URL is <c>/api/1/issues/org/table</c> below the portal application.
    /// </remarks>
    [Title("kleenestar.portal:api.issues.title")]
    [Cache]
    public sealed class Table : IRestApi
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Table()
        {
        }

        /// <summary>
        /// Handles <c>GET {base}</c>: returns the filtered, paged page of the
        /// organization's issues as a table result.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The table result as a JSON response.</returns>
        [Method(RequestMethod.GET)]
        public IResponse Retrieve(IRequest request)
        {
            return IssueTableProjection.Retrieve(IssueScope.Organization, request);
        }
    }
}
