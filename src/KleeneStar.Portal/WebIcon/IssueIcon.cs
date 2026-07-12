using WebExpress.WebCore.WebApplication;
using WebExpress.WebCore.WebUri;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebIcon;

namespace KleeneStar.Portal.WebIcon
{
    /// <summary>
    /// Represents the icon used for issue-scoped pages — list views, the detail drawer,
    /// and breadcrumb segments resolved through <see cref="WebUri.IssueKeyUriPathSegmentVariable{TParameter}"/>.
    /// </summary>
    public class IssueIcon : ImageIcon
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public IssueIcon()
            : base(new UriEndpoint("/portal/assets/img/issue.svg"), null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="applicationContext">The application context to be associated with the icon.</param>
        public IssueIcon(IApplicationContext applicationContext = null)
            : base(new UriEndpoint("/portal/assets/img/issue.svg"), null, applicationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="size">The size of the icon.</param>
        /// <param name="applicationContext">The application context to be associated with the icon.</param>
        public IssueIcon(PropertySizeIcon size, IApplicationContext applicationContext = null)
            : base(new UriEndpoint("/portal/assets/img/issue.svg"), size, applicationContext)
        {
        }
    }
}
