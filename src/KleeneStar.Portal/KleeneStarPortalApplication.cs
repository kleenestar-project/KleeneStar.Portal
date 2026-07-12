using WebExpress.WebCore;
using WebExpress.WebCore.WebApplication;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebComponent;
using WebExpress.WebCore.WebIcon;

namespace KleeneStar.Portal
{
    /// <summary>
    /// Represents the customer-facing KleeneStar portal application that runs alongside the
    /// operator WebApp inside the same host. It carries its own context path, navigation
    /// shell, and theme but shares the data layer and identity model with the operator
    /// application.
    /// </summary>
    [Name("kleenestar.portal:app.name")]
    [Description("kleenestar.portal:app.description")]
    [Icon("/assets/img/kleenestar.svg")]
    [IconTheme(TypeIconTheme.Light)]
    [ContextPath("/portal")]
    public sealed class KleeneStarPortalApplication : IApplication
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="applicationContext">The application context.</param>
        /// <param name="componentHub">The component hub.</param>
        /// <param name="httpServerContext">The reference to the context of the host.</param>
        public KleeneStarPortalApplication(IApplicationContext applicationContext, IComponentHub componentHub, IHttpServerContext httpServerContext)
        {
            PortalHub.HttpServerContext = httpServerContext;
            PortalHub.ComponentHub = componentHub;
            PortalHub.ApplicationContext = applicationContext;
        }

        /// <summary>
        /// Called when the application starts working. The call is concurrent.
        /// </summary>
        public void Run()
        {
        }
    }
}
