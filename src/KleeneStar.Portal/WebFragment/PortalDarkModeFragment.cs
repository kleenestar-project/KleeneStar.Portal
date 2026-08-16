using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebSection;
using WebExpress.WebCore.Internationalization;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebComponent;
using WebExpress.WebCore.WebFragment;
using WebExpress.WebCore.WebHtml;
using WebExpress.WebCore.WebScope;
using WebExpress.WebCore.WebIcon;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebFragment;
using WebExpress.WebUI.WebIcon;
using WebExpress.WebUI.WebPage;

namespace KleeneStar.Portal.WebFragment
{
    /// <summary>
    /// Dark mode toggle in the user avatar dropdown. Wires up the
    /// <see cref="ActionDarkmode"/> and a <see cref="BindDarkmode"/> binding so
    /// the client swaps the icon and the text between moon / sun.
    /// </summary>
    [Section<SectionAppAvatarPrimary>]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class PortalDarkModeFragment : FragmentControlDropdownItemLink
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="componentHub">The component hub used to manage components.</param>
        /// <param name="fragmentContext">The context in which the fragment is used.</param>
        public PortalDarkModeFragment(IComponentHub componentHub, IFragmentContext fragmentContext)
            : base(fragmentContext)
        {
            Icon = _ => new IconMoon(TypeIconTheme.Light);
            PrimaryAction = _ => new ActionDarkmode();
            Text = _ => "webexpress.webui:darkmode.label";
            Bind = ctx => new Binding().Add(new BindDarkmode
            {
                TextLight = I18N.Translate(ctx, "webexpress.webui:darkmode.text.light"),
                TextDark = I18N.Translate(ctx, "webexpress.webui:darkmode.text.dark")
            });
        }

        /// <summary>
        /// Convert the control to HTML.
        /// </summary>
        /// <param name="renderContext">The context in which the fragment is rendered.</param>
        /// <param name="visualTree">The visual tree representing the fragment's structure.</param>
        /// <returns>The rendered HTML node.</returns>
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
