using KleeneStar.Core.WebAttribute;
using KleeneStar.Core.WebParameter;
using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebScope;
using System;
using System.Linq;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW.Workspaces._workspacekey_.Classes._classid_.Statuses
{
    /// <summary>
    /// Statuses tab — read-only list of the active statuses of a class.
    /// </summary>
    [WebIcon<RequestTypeIcon>]
    [ClassIdSegment]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the headline.</summary>
        public const string HeadlineResource = "kleenestar.portal:admin.statuses.headline";

        /// <summary>Resource key for the empty-state message.</summary>
        public const string EmptyResource = "kleenestar.portal:admin.statuses.empty";

        /// <summary>Resource key for the name column.</summary>
        public const string NameColumnResource = "kleenestar.portal:admin.statuses.column.name";

        /// <summary>Resource key for the description column.</summary>
        public const string DescriptionColumnResource = "kleenestar.portal:admin.statuses.column.description";

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
            var classIdParameter = renderContext.Request.GetParameter<ClassIdParameter>();
            if (classIdParameter is null || !Guid.TryParse(classIdParameter.Value, out var classId)
                || _portalManager.GetClass(classId) is null)
            {
                visualTree.Title = HeadlineResource;
                visualTree.Content.MainPanel.Headline.Title = HeadlineResource;
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => "kleenestar.portal:admin.class.not-found",
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            var statuses = _portalManager.GetStatuses(classId);
            visualTree.Title = HeadlineResource;
            visualTree.Content.MainPanel.Headline.Title = HeadlineResource;

            if (statuses.Count == 0)
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
                .AddColumn(DescriptionColumnResource);

            foreach (var status in statuses)
            {
                var capturedName = status.Name;
                var capturedDescription = status.Description ?? string.Empty;

                table = table.AddRow
                (
                    new ControlTableCell() { Text = _ => capturedName },
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
