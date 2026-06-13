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

namespace KleeneStar.Portal.WWW.Workspaces._workspacekey_
{
    /// <summary>
    /// Workspace detail page — lists the workspace's classes as a tile grid. The
    /// <c>{workspacekey}</c> URL segment is bound by
    /// <see cref="WorkspaceKeySegmentAttribute"/>.
    /// </summary>
    [WebIcon<PortalIcon>]
    [WorkspaceKeySegment]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the headline (workspace name).</summary>
        public const string HeadlineResource = "kleenestar.portal:admin.workspace.headline";

        /// <summary>Resource key for the empty-state message.</summary>
        public const string EmptyResource = "kleenestar.portal:admin.workspace.empty";

        /// <summary>Resource key for the description placeholder.</summary>
        public const string DescriptionResource = "kleenestar.portal:admin.workspace.description";

        /// <summary>Resource key for the classes section.</summary>
        public const string ClassesSectionResource = "kleenestar.portal:admin.workspace.classes";

        /// <summary>Resource key for the portal-visible badge.</summary>
        public const string PortalVisibleResource = "kleenestar.portal:admin.class.badge.portal-visible";

        /// <summary>Resource key for the internal badge.</summary>
        public const string InternalResource = "kleenestar.portal:admin.class.badge.internal";

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

            visualTree.Title = workspace?.Name ?? keyParameter?.Value;
            visualTree.Content.MainPanel.Headline.Title = workspace?.Name ?? keyParameter?.Value;

            if (workspace is null)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => "kleenestar.portal:admin.workspace.not-found",
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => workspace.Description ?? string.Empty,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
            });

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => ClassesSectionResource,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Three, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });

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

            var grid = new ControlPanel("portal-workspace-classes")
            {
                Direction = _ => TypeDirection.Horizontal
            };

            foreach (var cls in classes)
            {
                var capturedId = cls.Id;
                var capturedName = cls.Name;
                var capturedDescription = cls.Description ?? string.Empty;
                var badge = cls.PortalVisible ? PortalVisibleResource : InternalResource;

                var tile = new ControlPanelCard($"portal-class-tile-{cls.Id}")
                {
                    Header = _ => capturedName,
                    Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.One, PropertySpacing.Space.One, PropertySpacing.Space.One, PropertySpacing.Space.One)
                };
                tile.Add(new ControlLink()
                {
                    Text = _ => "kleenestar.portal:admin.workspace.open",
                    Uri = _ => PortalHub.GetUri<global::KleeneStar.Portal.WWW.Workspaces._workspacekey_.Classes._classid_.Index>()
                        ?.BindParameters(new WorkspaceKeyParameter(keyParameter?.Value))
                        ?.BindParameters(new ClassIdParameter(capturedId.ToString()))
                });
                tile.Add(new ControlText()
                {
                    Text = _ => capturedDescription,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                tile.Add(new ControlText()
                {
                    Text = _ => badge,
                    TextColor = _ => new PropertyColorText(cls.PortalVisible ? TypeColorText.Primary : TypeColorText.Secondary)
                });

                grid.Add(tile);
            }

            visualTree.Content.MainPanel.AddPrimary(grid);
        }
    }
}
