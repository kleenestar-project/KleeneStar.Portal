using KleeneStar.Core;
using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebCondition;
using WebExpress.WebApp.WebControl;
using WebExpress.WebApp.WebData;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebPage;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// The login dialog that <see cref="PortalLoginLinkFragment"/> (the Login entry of the
    /// portal's avatar menu) opens, so a customer signs in on top of the page they are on.
    /// </summary>
    /// <remarks>
    /// The dialog is the framework's <see cref="ControlDataModalLogin"/> bound to the shared
    /// session endpoint <see cref="KleeneStar.Core.WWW.Api._1_.Session"/> - the portal identity
    /// model is the operator-side one, and the portal's full-page login at
    /// <see cref="WWW.Session.Index"/> posts to the same place. The portal does not share the
    /// operator scopes, so the core's dialog never reaches its pages; this one is scoped to the
    /// portal and carries the same well-known id, so the link is authored identically.
    /// <para>
    /// The endpoint is resolved through <see cref="CoreHub"/> rather than declared with
    /// <c>DataService&lt;Session&gt;()</c>: the authoring preset resolves a route against the
    /// application of the page being rendered, and the portal application does not carry the
    /// core's session route - the preset would answer <c>/</c> and the credentials would be
    /// posted at the site root. The full-page login resolves it the same way.
    /// </para>
    /// </remarks>
    [Section<SectionBodySecondary>]
    [Scope<IScopePortal>]
    [Condition<ConditionLogout>]
    [Cache]
    public sealed class PortalLoginModalFragment : ControlDataModalLogin, IFragmentControl<ControlDataModalLogin>
    {
        /// <summary>
        /// Gets the context of the fragment.
        /// </summary>
        public IFragmentContext FragmentContext { get; }

        /// <summary>
        /// Initializes a new instance of the class with the well-known <c>modal-login</c> id,
        /// so the avatar Login link can target it.
        /// </summary>
        /// <param name="fragmentContext">The context in which the fragment is used.</param>
        public PortalLoginModalFragment(IFragmentContext fragmentContext)
            : base("modal-login")
        {
            FragmentContext = fragmentContext;
            Header = _ => "kleenestar.portal:login.title";

            ServiceFactory = _ => DataServiceDescriptor.SubmitData
            (
                CoreHub.GetUri<global::KleeneStar.Core.WWW.Api._1_.Session>()?.ToString()
            );
        }

        /// <summary>
        /// Renders the control as an HTML node.
        /// </summary>
        /// <param name="renderContext">The context in which the control is rendered.</param>
        /// <param name="visualTree">The visual tree representing the control's structure.</param>
        /// <returns>
        /// An HTML node representing the rendered control, or null when the user is signed in.
        /// </returns>
        public override IHtmlNode Render(IRenderControlContext renderContext, IVisualTreeControl visualTree)
        {
            if (!FragmentContext.Conditions.Check(renderContext?.Request))
            {
                return null;
            }

            return base.Render(renderContext, visualTree);
        }
    }
}
