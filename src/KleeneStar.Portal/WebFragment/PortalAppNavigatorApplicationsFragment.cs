using KleeneStar.Core.WebFragment.AppNavigator;
using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebComponent;
using WebExpress.WebCore.WebFragment;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Contributes the list of installed applications to the preferences area of the app navigator
    /// of the portal application.
    /// </summary>
    /// <remarks>
    /// The portal needs its own registration because a fragment only contributes to the application
    /// whose plugin declares it. Without it the portal navigator stays a plain image and the user has
    /// no way back to the other applications.
    /// </remarks>
    [Section<SectionAppPreferences>]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class PortalAppNavigatorApplicationsFragment : AppNavigatorApplicationsFragmentBase
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="componentHub">The component hub used to resolve the installed applications.</param>
        /// <param name="fragmentContext">The context in which the fragment is used.</param>
        public PortalAppNavigatorApplicationsFragment(IComponentHub componentHub, IFragmentContext fragmentContext)
            : base(componentHub, fragmentContext)
        {
        }
    }
}
