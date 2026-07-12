using WebExpress.WebCore.WebApplication;
using WebExpress.WebCore.WebUri;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebIcon;

namespace KleeneStar.Portal.WebIcon
{
    /// <summary>
    /// Represents the icon used for the request-type catalog. Rendered next to the home
    /// page entry in the navigation shell.
    /// </summary>
    public class RequestTypeIcon : ImageIcon
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public RequestTypeIcon()
            : base(new UriEndpoint("/portal/assets/img/requesttype.svg"), null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="applicationContext">The application context to be associated with the icon.</param>
        public RequestTypeIcon(IApplicationContext applicationContext = null)
            : base(new UriEndpoint("/portal/assets/img/requesttype.svg"), null, applicationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="size">The size of the icon.</param>
        /// <param name="applicationContext">The application context to be associated with the icon.</param>
        public RequestTypeIcon(PropertySizeIcon size, IApplicationContext applicationContext = null)
            : base(new UriEndpoint("/portal/assets/img/requesttype.svg"), size, applicationContext)
        {
        }
    }
}
