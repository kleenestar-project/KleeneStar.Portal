using KleeneStar.Core.WebFragment;
using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// The header avatar of the portal: the name and the profile picture of whoever is signed
    /// in. The portal runs as its own application, so the core's fragment does not reach its
    /// pages; see <see cref="SessionAvatarFragmentBase"/>.
    /// </summary>
    [Section<SectionAppAvatar>]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class PortalHeaderAvatarFragment : SessionAvatarFragmentBase
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalHeaderAvatarFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
        }
    }
}
