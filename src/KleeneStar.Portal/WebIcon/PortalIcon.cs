using WebExpress.WebCore.WebApplication;
using WebExpress.WebCore.WebUri;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebIcon;

namespace KleeneStar.Portal.WebIcon
{
    /// <summary>
    /// Brand mark of the customer portal. Renders the shared KleeneStar glyph from the
    /// portal's own asset path so the customer never browses to the operator-side URL.
    /// </summary>
    public class PortalIcon : ImageIcon
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public PortalIcon()
            : base(new UriEndpoint("/portal/assets/img/kleenestar.svg"), null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="applicationContext">The application context to be associated with the icon.</param>
        public PortalIcon(IApplicationContext applicationContext = null)
            : base(new UriEndpoint("/portal/assets/img/kleenestar.svg"), null, applicationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="size">The size of the icon.</param>
        /// <param name="applicationContext">The application context to be associated with the icon.</param>
        public PortalIcon(PropertySizeIcon size, IApplicationContext applicationContext = null)
            : base(new UriEndpoint("/portal/assets/img/kleenestar.svg"), size, applicationContext)
        {
        }
    }
}
