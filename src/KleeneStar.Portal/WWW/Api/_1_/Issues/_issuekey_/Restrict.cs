using KleeneStar.Portal.WebManager;
using System.Collections.Generic;
using System.Text.Json;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Issues._issuekey_
{
    /// <summary>
    /// REST endpoint over who sees an issue: its participants only, or the requester's
    /// organization. The URL is <c>/api/1/issues/{issuekey}/restrict</c> below the portal
    /// application; the <c>{issuekey}</c> segment is declared by the sibling
    /// <see cref="Index"/> endpoint, so this class must NOT carry the segment attribute itself.
    /// </summary>
    [Title("kleenestar.portal:api.issue.restrict.title")]
    [Cache]
    public sealed class Restrict : IRestApi
    {
        /// <summary>
        /// Gets the portal manager resolved through the <see cref="PortalHub"/> facade.
        /// </summary>
        private static IPortalManager _portalManager => PortalHub.PortalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Restrict()
        {
        }

        /// <summary>
        /// Handles <c>POST {base}</c>: keeps the issue to its participants - or, with a body
        /// <c>{ "restricted": false }</c>, opens it to the organization again. The body is how
        /// the portal's own form says which way it goes; a client may use <c>DELETE</c> instead.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns><c>204 No Content</c>, <c>403</c> when the caller may not change the
        /// audience, or <c>404</c> when the issue is not visible.</returns>
        [Method(RequestMethod.POST)]
        public IResponse Create(IRequest request)
        {
            return Set(request, ReadRestricted(request) ?? true);
        }

        /// <summary>
        /// Handles <c>DELETE {base}</c>: opens the issue to the requester's organization again.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns><c>204 No Content</c>, <c>403</c> when the caller may not change the
        /// audience, or <c>404</c> when the issue is not visible.</returns>
        [Method(RequestMethod.DELETE)]
        public IResponse Delete(IRequest request)
        {
            return Set(request, false);
        }

        /// <summary>
        /// Reads the requested audience from the body, accepting a JSON boolean or the string a
        /// form's hidden input sends.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The requested state, or <see langword="null"/> when the body names none.</returns>
        private static bool? ReadRestricted(IRequest request)
        {
            if (!PortalApi.TryReadBody<Dictionary<string, JsonElement>>(request, out var body)
                || !body.TryGetValue("restricted", out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
                _ => null
            };
        }

        /// <summary>
        /// Applies the audience and maps the outcome to its status.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <param name="restricted">Whether the issue is kept to its participants.</param>
        /// <returns>The response.</returns>
        private static IResponse Set(IRequest request, bool restricted)
        {
            return PortalApi.RunWithConflictMapping(() =>
            {
                var issue = _portalManager.SetRestricted(PortalApi.GetIssueKey(request), restricted);

                return issue is null ? PortalApi.NotFound() : PortalApi.NoContent();
            });
        }
    }
}
