using KleeneStar.Portal.WebParameter;
using KleeneStar.Portal.WebUri;
using System;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebUri;

namespace KleeneStar.Portal.WebAttribute
{
    /// <summary>
    /// Specifies a request-type key for use in endpoint routing, associating
    /// <see cref="RequestTypeKeyParameter"/> with a variable path segment under the portal.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RequestTypeKeySegmentAttribute : Attribute, IEndpointAttribute, ISegmentAttribute
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public RequestTypeKeySegmentAttribute()
        {
        }

        /// <summary>
        /// Conversion to a path segment.
        /// </summary>
        /// <returns>The path segment.</returns>
        public IUriPathSegment ToPathSegment()
        {
            return new RequestTypeKeyUriPathSegmentVariable<RequestTypeKeyParameter>();
        }
    }
}
