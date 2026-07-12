using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebScope;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.Internationalization;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;
using WebExpress.WebUI.WebIcon;

namespace KleeneStar.Portal.WWW.Profile
{
    /// <summary>
    /// Profile landing page for the customer portal. Display-only view of the
    /// caller's identity data (display name and email) — the operator-side
    /// <c>WWW/Profile/Index.cs</c> exposes a much wider field set, but a
    /// customer portal has no need for bio, position, website, or location. No
    /// edit form lives here; profile mutations are out of scope for the
    /// portal plugin and stay with the operator WebApp.
    /// </summary>
    [WebIcon<IconCircleUser>]
    [Title("kleenestar.portal:profile.title")]
    [Scope<IScopePortal>]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        private readonly IPortalManager _portalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="portalManager">The portal manager.</param>
        public Index(IPortalManager portalManager)
        {
            _portalManager = portalManager;
        }

        /// <summary>
        /// Processing of the resource.
        /// </summary>
        /// <param name="renderContext">The context for rendering the page.</param>
        /// <param name="visualTree">The visual tree of the web application.</param>
        public void Process(IRenderContext renderContext, VisualTreeWebApp visualTree)
        {
            var current = _portalManager.CurrentUser;

            visualTree.Title = I18N.Translate(renderContext, "kleenestar.portal:profile.title");
            visualTree.Content.MainPanel.Headline.Title = I18N.Translate(renderContext, "kleenestar.portal:profile.header");

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => I18N.Translate(renderContext, "kleenestar.portal:profile.description"),
                TextColor = _ => new PropertyColorText(TypeColorText.Info),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Two)
            });

            var table = new ControlTable()
            {
                Striped = _ => TypeStripedTable.Row,
                SuppressHeaders = _ => true
            }
                .AddColumn("")
                .AddColumn("");

            AddRow(table, renderContext, "kleenestar.portal:profile.field.displayname.label", current?.Name);
            AddRow(table, renderContext, "kleenestar.portal:profile.field.email.label", current?.Email);

            visualTree.Content.MainPanel.AddPrimary(table);
        }

        /// <summary>
        /// Adds a row with a translated label and value to the control table.
        /// </summary>
        /// <param name="table">The control table to add the row to.</param>
        /// <param name="renderContext">The render context used for translating the label.</param>
        /// <param name="labelKey">The translation key for the label cell.</param>
        /// <param name="value">The raw value for the second cell. <see langword="null"/> renders as a dash.</param>
        private static void AddRow(IControlTable table, IRenderContext renderContext, string labelKey, string value)
        {
            var display = string.IsNullOrWhiteSpace(value)
                ? I18N.Translate(renderContext, "kleenestar.portal:common.na")
                : value;

            table.AddRow
            (
                new ControlTableCell() { Text = _ => I18N.Translate(renderContext, labelKey) },
                new ControlTableCellPanel().Add(new ControlText() { Text = _ => display })
            );
        }
    }
}
