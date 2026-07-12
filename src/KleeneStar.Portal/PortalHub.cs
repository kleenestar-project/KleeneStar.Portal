using KleeneStar.Portal.WebManager;
using WebExpress.WebCore;
using WebExpress.WebCore.WebApplication;
using WebExpress.WebCore.WebComponent;
using WebExpress.WebCore.WebEndpoint;
using WebExpress.WebCore.WebParameter;
using WebExpress.WebCore.WebUri;

namespace KleeneStar.Portal
{
    /// <summary>
    /// Provides utility methods and shared state for the KleeneStar customer portal — the
    /// portal-side equivalent of <c>CoreHub</c>. Portal pages, fragments, and managers reach
    /// for the registered <see cref="IPortalManager"/> through this facade.
    /// </summary>
    public static class PortalHub
    {
        private static PortalManager _portalManager;

        /// <summary>
        /// Gets the shared instance of the component hub used for managing and coordinating application components.
        /// </summary>
        public static IComponentHub ComponentHub { get; internal set; }

        /// <summary>
        /// Gets the current application context, which provides access to application-wide services and configurations.
        /// </summary>
        public static IApplicationContext ApplicationContext { get; internal set; }

        /// <summary>
        /// Gets the current HTTP server context for the portal application.
        /// </summary>
        public static IHttpServerContext HttpServerContext { get; internal set; }

        /// <summary>
        /// Gets the portal manager that orchestrates the portal-visible projection of the
        /// underlying object model — request types, issues, sharing, watching, resolution.
        /// </summary>
        public static IPortalManager PortalManager => _portalManager ??= ComponentHub.GetComponentManager<PortalManager>();

        /// <summary>
        /// Constructs a URI for the specified portal endpoint type using the provided parameters.
        /// </summary>
        /// <typeparam name="TEndpoint">The type of the endpoint for which the URI is being constructed.</typeparam>
        /// <param name="parameters">An array of parameters used to customize the URI construction. Can be empty.</param>
        /// <returns>The URI bound to the portal application.</returns>
        public static IUri GetUri<TEndpoint>(params Parameter[] parameters)
            where TEndpoint : IEndpoint
        {
            return ComponentHub.SitemapManager.GetUri<TEndpoint>(ApplicationContext, parameters);
        }
    }
}
