using WebExpress.WebCore;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPlugin;

namespace KleeneStar.Portal
{
    /// <summary>
    /// Plugin entry point that registers the customer portal application with the host.
    /// </summary>
    [Name("kleenestar.portal:plugin.name")]
    [Description("kleenestar.portal:plugin.description")]
    [Icon("/portal/assets/img/kleenestar.svg")]
    [Application<KleeneStarPortalApplication>()]
    [Dependency("webexpress.webapp")]
    [Dependency("kleenestar.core")]
    public sealed class KleeneStarPlugin : IPlugin
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public KleeneStarPlugin()
        {
            WebEx.Favicon = "/portal/assets/img/kleenestar.ico";
        }

        /// <summary>
        /// Called when the plugin starts working. Run is called concurrently.
        /// </summary>
        public void Run()
        {
        }
    }
}
