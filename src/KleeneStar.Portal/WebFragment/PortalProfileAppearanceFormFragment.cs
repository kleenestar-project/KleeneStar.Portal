using KleeneStar.Core.WebFragment.Appearance;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// The adjustments card of the portal profile - accent, font size, density, motion - over
    /// <c>/portal/api/1/appearance</c> (<see cref="AppearanceFormFragmentBase{TEndpoint}"/>).
    /// </summary>
    [Section<SectionContentPrimary>]
    [Scope<global::KleeneStar.Portal.WWW.Profile.Index>]
    [Order(20)]
    [Cache]
    public sealed class PortalProfileAppearanceFormFragment : AppearanceFormFragmentBase<global::KleeneStar.Portal.WWW.Api._1_.Appearance.Index>
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalProfileAppearanceFormFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
        }
    }
}
