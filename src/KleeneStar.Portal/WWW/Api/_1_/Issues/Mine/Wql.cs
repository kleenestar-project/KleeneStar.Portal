using WebExpress.WebCore.WebAttribute;

namespace KleeneStar.Portal.WWW.Api._1_.Issues.Mine
{
    /// <summary>
    /// Advanced-search prompt endpoint for the "My Issues" view. The history and
    /// lookahead behaviour is provided by <see cref="IssueWqlBase"/>; this leaf type
    /// exists only to expose the route <c>/api/1/issues/mine/wql</c>.
    /// </summary>
    [Cache]
    public sealed class Wql : IssueWqlBase
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Wql()
        {
        }
    }
}
