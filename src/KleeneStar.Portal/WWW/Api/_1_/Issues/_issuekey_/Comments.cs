using KleeneStar.Portal.WebManager;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebCore.WebRestApi;

namespace KleeneStar.Portal.WWW.Api._1_.Issues._issuekey_
{
    /// <summary>
    /// REST endpoint appending a comment to an issue's timeline. The URL is
    /// <c>/api/1/issues/{issuekey}/comments</c> below the portal application; the
    /// <c>{issuekey}</c> segment is declared by the sibling <see cref="Index"/>
    /// endpoint, so this class must NOT carry the segment attribute itself.
    /// </summary>
    /// <remarks>
    /// The body carries <c>text</c> (required) and <c>visibility</c> (<c>public</c> —
    /// the default — or <c>internal-team</c>). The core comment model does not persist
    /// a visibility flag yet, so the value is accepted for forward compatibility but
    /// every stored comment is public (see the roadmap document).
    /// </remarks>
    [Title("kleenestar.portal:api.issue.comments.title")]
    [Cache]
    public sealed class Comments : IRestApi
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
        public Comments()
        {
        }

        /// <summary>
        /// Handles <c>POST {base}</c>: appends a comment to the addressed issue.
        /// </summary>
        /// <param name="request">The incoming request carrying the JSON body.</param>
        /// <returns>
        /// <c>201 Created</c> with the updated issue projection, <c>400</c> when the
        /// text is missing, or <c>404</c> when the issue is not visible.
        /// </returns>
        [Method(RequestMethod.POST)]
        public IResponse Create(IRequest request)
        {
            if (!PortalApi.TryReadBody<CommentPayload>(request, out var payload)
                || string.IsNullOrWhiteSpace(payload.Text))
            {
                return PortalApi.Error("'text' is required.");
            }

            var issue = _portalManager.AddComment(
                PortalApi.GetIssueKey(request),
                payload.Text,
                payload.Visibility ?? "public");

            return issue is null
                ? PortalApi.NotFound()
                : PortalApi.Created(PortalApi.ToDto(issue, includeTimeline: true));
        }

        /// <summary>
        /// The JSON body accepted by <c>POST</c>.
        /// </summary>
        private sealed class CommentPayload
        {
            /// <summary>Gets or sets the message body.</summary>
            public string Text { get; set; }

            /// <summary>Gets or sets the visibility flag (<c>public</c> or <c>internal-team</c>).</summary>
            public string Visibility { get; set; }
        }
    }
}
