using KleeneStar.Core.WebFragment;
using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Tells every portal page when its sign-in's access token ends, so the page renews it in
    /// time (see <see cref="SessionMetaFragmentBase"/>). A fragment is registered per
    /// application, so the portal needs its own beside the core's.
    /// </summary>
    [Section<SectionContentPrimary>]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class PortalSessionMetaFragment : SessionMetaFragmentBase
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fragmentContext">The context of the fragment.</param>
        public PortalSessionMetaFragment(IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
        }
    }
}
