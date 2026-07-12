using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebScope;
using KleeneStar.Model.Entities;
using System;
using System.Linq;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW.Workspaces
{
    /// <summary>
    /// Administration landing page — the workspace overview. Lists the
    /// workspaces the calling identity is allowed to administer (tenant-scoped
    /// for portal users, every active workspace for operator-side identities).
    /// </summary>
    [WebIcon<PortalIcon>]
    [Title("kleenestar.portal:admin.workspaces.title")]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the headline.</summary>
        public const string HeadlineResource = "kleenestar.portal:admin.workspaces.headline";

        /// <summary>Resource key for the empty-state message.</summary>
        public const string EmptyResource = "kleenestar.portal:admin.workspaces.empty";

        /// <summary>Resource key for the key column.</summary>
        public const string KeyColumnResource = "kleenestar.portal:admin.workspaces.column.key";

        /// <summary>Resource key for the name column.</summary>
        public const string NameColumnResource = "kleenestar.portal:admin.workspaces.column.name";

        /// <summary>Resource key for the classes column.</summary>
        public const string ClassesColumnResource = "kleenestar.portal:admin.workspaces.column.classes";

        /// <summary>Resource key for the description column.</summary>
        public const string DescriptionColumnResource = "kleenestar.portal:admin.workspaces.column.description";

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
            visualTree.Title = HeadlineResource;
            visualTree.Content.MainPanel.Headline.Title = HeadlineResource;

            var workspaces = _portalManager.GetWorkspaces();
            if (workspaces.Count == 0)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => EmptyResource,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            var table = new ControlTable()
            {
                Striped = _ => TypeStripedTable.Row
            }
                .AddColumn(KeyColumnResource)
                .AddColumn(NameColumnResource)
                .AddColumn(ClassesColumnResource)
                .AddColumn(DescriptionColumnResource);

            foreach (var workspace in workspaces)
            {
                var capturedKey = workspace.Key;
                var capturedName = workspace.Name;
                var capturedDescription = workspace.Description ?? string.Empty;
                var classCount = _portalManager.GetClasses(workspace.Id).Count;
                var capturedCount = classCount;

                table = table.AddRow
                (
                    new ControlTableCell() { Text = _ => capturedKey },
                    new ControlTableCellPanel().Add(new ControlLink()
                    {
                        Text = _ => capturedName,
                        Uri = _ => PortalHub.GetUri<_workspacekey_.Index>()?.BindParameters(new KleeneStar.Core.WebParameter.WorkspaceKeyParameter(capturedKey))
                    }),
                    new ControlTableCell() { Text = _ => capturedCount.ToString() },
                    new ControlTableCellPanel().Add(new ControlText()
                    {
                        Text = _ => capturedDescription,
                        TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                    })
                );
            }

            visualTree.Content.MainPanel.AddPrimary(table);
        }
    }
}
