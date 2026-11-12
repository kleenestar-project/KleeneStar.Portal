using KleeneStar.Core.WebFragment.Appearance;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// The theme card of the portal profile: theme and mode, the framework's selectors over the
    /// portal's endpoints (<see cref="AppearanceThemeCardFragmentBase"/>). The themes offered are
    /// those registered for the portal - its default and those of <c>KleeneStar.Theme</c>.
    /// </summary>
    [Section<SectionContentPrimary>]
    [Scope<global::KleeneStar.Portal.WWW.Profile.Index>]
    [Order(10)]
    [Cache]
    public sealed class PortalProfileAppearanceThemeFragment : AppearanceThemeCardFragmentBase
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalProfileAppearanceThemeFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
        }
    }
}
