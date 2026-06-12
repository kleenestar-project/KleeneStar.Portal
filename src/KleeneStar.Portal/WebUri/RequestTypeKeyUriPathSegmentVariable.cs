using KleeneStar.Portal.WebParameter;
using WebExpress.WebCore.WebParameter;
using WebExpress.WebCore.WebUri;

namespace KleeneStar.Portal.WebUri
{
    /// <summary>
    /// Variable path segment that captures a request-type slug (e.g. <c>report-incident</c>).
    /// The regex matches the lower-case, dash-separated slugs that
    /// <c>PortalManager</c> produces from class display names.
    /// </summary>
    /// <typeparam name="TParameter">The parameter type.</typeparam>
    public class RequestTypeKeyUriPathSegmentVariable<TParameter> : UriPathSegmentVariableRegex<TParameter>
        where TParameter : IParameterStatic, new()
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="tag">The tag or null.</param>
        public RequestTypeKeyUriPathSegmentVariable(object tag = null)
            : base(@"^[a-z0-9]+(-[a-z0-9]+)*$", tag)
        {
        }

        /// <summary>
        /// Make a deep copy.
        /// </summary>
        /// <returns>The copy.</returns>
        public override IUriPathSegment Copy()
        {
            return new RequestTypeKeyUriPathSegmentVariable<TParameter>()
            {
                Expression = Expression,
                Value = Value,
                IsHidden = IsHidden,
                Uri = Uri
            };
        }
    }
}
