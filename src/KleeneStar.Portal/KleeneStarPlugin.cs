using KleeneStar.Core;
using WebExpress.WebCore;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPlugin;

namespace KleeneStar.Portal
{
    [Name("kleenestar.core:plugin.name")]
    [Description("kleenestar.core:plugin.description")]
    [Icon("/assets/img/kleenestar.svg")]
    [Application<KleeneStarApplication>()]
    [Dependency("webexpress.webapp")]
    public sealed class KleeneStarPlugin : IPlugin
    {
        /// <summary>  
        /// Initializes a new instance of the class.  
        /// </summary>  
        public KleeneStarPlugin()
        {
            WebEx.Favicon = "/assets/img/kleenestar.ico";
        }

        /// <summary>  
        /// Called when the plugin starts working. Run is called concurrently.  
        /// </summary>  
        public void Run()
        {
        }
    }
}
