using WebExpress.WebCore.WebIcon;
using WebExpress.WebCore.WebPage;
using WebExpress.WebCore.WebParameter;
using WebExpress.WebCore.WebUri;

namespace KleeneStar.Portal.WebUri
{
    /// <summary>
    /// Variable path segment that captures an issue key. Mirrors
    /// <c>WorkspaceKeyUriPathSegmentVariable</c> but uses a regex tuned for keys of the form
    /// <c>ABC-123</c> (typical: <c>INC-2041</c>, <c>REQ-1284</c>).
    /// </summary>
    /// <typeparam name="TParameter">The parameter type.</typeparam>
    public class IssueKeyUriPathSegmentVariable<TParameter> : UriPathSegmentVariableRegex<TParameter>
        where TParameter : IParameterStatic, new()
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="tag">The tag or null.</param>
        public IssueKeyUriPathSegmentVariable(object tag = null)
            : base(@"^[A-Z]{2,4}-[0-9]{1,8}$", tag)
        {
        }

        /// <summary>
        /// Make a deep copy.
        /// </summary>
        /// <returns>The copy.</returns>
        public override IUriPathSegment Copy()
        {
            return new IssueKeyUriPathSegmentVariable<TParameter>()
            {
                Expression = Expression,
                Value = Value,
                IsHidden = IsHidden,
                Uri = Uri
            };
        }

        /// <summary>
        /// Returns the display text — the issue title resolved through the portal manager.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        /// <returns>The display text, or the raw key when the issue cannot be resolved.</returns>
        public override string GetDisplayText(IRenderContext renderContext)
        {
            var issue = PortalHub.PortalManager?.GetIssue(Value);
            return issue?.Title ?? Value;
        }

        /// <summary>
        /// Returns the icon associated with the issue, if any. The portal does not currently
        /// surface a per-issue icon, so the segment renders without one.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        /// <returns>Always <c>null</c>.</returns>
        public override IIcon GetIcon(IRenderContext renderContext)
        {
            return null;
        }
    }
}
