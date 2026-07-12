using KleeneStar.Core.WebAttribute;
using KleeneStar.Core.WebParameter;
using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebScope;
using System.Linq;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW.Workspaces._workspacekey_.Classes
{
    /// <summary>
    /// Class list page — a tabular overview of the workspace's classes. The
    /// <c>{workspacekey}</c> URL segment is bound by
    /// <see cref="WorkspaceKeySegmentAttribute"/>.
    /// </summary>
    [WebIcon<RequestTypeIcon>]
    [WorkspaceKeySegment]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the headline.</summary>
        public const string HeadlineResource = "kleenestar.portal:admin.classes.headline";

        /// <summary>Resource key for the empty-state message.</summary>
        public const string EmptyResource = "kleenestar.portal:admin.classes.empty";

        /// <summary>Resource key for the name column.</summary>
        public const string NameColumnResource = "kleenestar.portal:admin.classes.column.name";

        /// <summary>Resource key for the description column.</summary>
        public const string DescriptionColumnResource = "kleenestar.portal:admin.classes.column.description";

        /// <summary>Resource key for the visibility column.</summary>
        public const string VisibilityColumnResource = "kleenestar.portal:admin.classes.column.visibility";

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
            var keyParameter = renderContext.Request.GetParameter<WorkspaceKeyParameter>();
            var workspace = _portalManager.GetWorkspace(keyParameter?.Value);

            visualTree.Title = workspace is null
                ? HeadlineResource
                : $"{workspace.Name} — {HeadlineResource}";
            visualTree.Content.MainPanel.Headline.Title = HeadlineResource;

            if (workspace is null)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => "kleenestar.portal:admin.workspace.not-found",
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            var classes = _portalManager.GetClasses(workspace.Id);
            if (classes.Count == 0)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => EmptyResource,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            var table = new ControlTable() { Striped = _ => TypeStripedTable.Row }
                .AddColumn(NameColumnResource)
                .AddColumn(DescriptionColumnResource)
                .AddColumn(VisibilityColumnResource);

            foreach (var cls in classes)
            {
                var capturedId = cls.Id;
                var capturedName = cls.Name;
                var capturedDescription = cls.Description ?? string.Empty;

                table = table.AddRow
                (
                    new ControlTableCellPanel().Add(new ControlLink()
                    {
                        Text = _ => capturedName,
                        Uri = _ => PortalHub
                            .GetUri<_classid_.Index>()
                            ?.BindParameters(new WorkspaceKeyParameter(keyParameter?.Value))
                            ?.BindParameters(new ClassIdParameter(capturedId.ToString()))
                    }),
                    new ControlTableCellPanel().Add(new ControlText()
                    {
                        Text = _ => capturedDescription,
                        TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                    }),
                    new ControlTableCell() { Text = _ => cls.PortalVisible ? "✓" : "—" }
                );
            }

            visualTree.Content.MainPanel.AddPrimary(table);
        }
    }
}
