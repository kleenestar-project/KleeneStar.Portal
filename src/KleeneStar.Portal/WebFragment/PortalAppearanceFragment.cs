using KleeneStar.Core.WebFragment.Appearance;
using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Draws every portal page in the appearance its reader chose - theme and adjustments; the
    /// behaviour is the core's <see cref="AppearanceFragmentBase"/>. A fragment is registered
    /// per application, so the core's own does not reach the portal.
    /// </summary>
    [Section<SectionBodyPreferences>]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class PortalAppearanceFragment : AppearanceFragmentBase
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalAppearanceFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
        }
    }
}
